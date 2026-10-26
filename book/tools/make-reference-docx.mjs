// Generates book/assets/reference.docx, the style template for the Word edition.
//
//   node book/tools/make-reference-docx.mjs
//
// It starts from pandoc's own reference.docx and patches its styles, page setup
// and footer, so the template stays in step with the pandoc version that uses
// it. The result is committed; rerun this only when the styles change.

import { execFileSync } from 'node:child_process';
import { writeFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

import { readZip, writeZip } from './zip.mjs';

const here = dirname(fileURLToPath(import.meta.url));
const output = resolve(here, '..', 'assets', 'reference.docx');

// ---- palette and type (matches assets/book.css) ----------------------------
const navy = '14213D';
const ink = '1D2733';
const slate = '435163';
const link = '1F5FBF';
const rule = 'DFE3E8';
const font = 'Calibri';
const mono = 'Consolas';

// ---- style helpers ---------------------------------------------------------
const fonts = (name) => `<w:rFonts w:ascii="${name}" w:hAnsi="${name}" w:eastAsia="${name}" w:cs="${name}"/>`;
const size = (halfPoints) => `<w:sz w:val="${halfPoints}"/><w:szCs w:val="${halfPoints}"/>`;
const color = (hex) => `<w:color w:val="${hex}"/>`;
const shade = (hex) => `<w:shd w:val="clear" w:color="auto" w:fill="${hex}"/>`;
const border = (side, hex, width = 4, space = 4) => `<w:${side} w:val="single" w:sz="${width}" w:space="${space}" w:color="${hex}"/>`;

function paragraphStyle(id, name, { basedOn = 'BodyText', next, pPr = '', rPr = '' } = {}) {
  // toc 1, toc 2 ... are built-in styles; everything else here is custom.
  const custom = id.startsWith('TOC') ? '' : ' w:customStyle="1"';
  return `<w:style w:type="paragraph"${custom} w:styleId="${id}"><w:name w:val="${name}"/>`
    + `<w:basedOn w:val="${basedOn}"/>${next ? `<w:next w:val="${next}"/>` : ''}<w:qFormat/>`
    + `<w:pPr>${pPr}</w:pPr><w:rPr>${rPr}</w:rPr></w:style>`;
}

// Replace the pPr/rPr of an existing style (or add the style if it is missing).
function restyle(xml, id, { pPr, rPr, extra = '' }) {
  const pattern = new RegExp(`<w:style\\b[^>]*w:styleId="${id}"[^>]*>[\\s\\S]*?</w:style>`);
  const match = xml.match(pattern);
  if (!match) throw new Error(`style ${id} not found`);
  let style = match[0]
    .replace(/<w:pPr>[\s\S]*?<\/w:pPr>|<w:pPr\s*\/>/, '')
    .replace(/<w:rPr>[\s\S]*?<\/w:rPr>|<w:rPr\s*\/>/, '')
    .replace(/<w:semiHidden\s*\/>/, '');
  style = style.replace('</w:style>', `${extra}${pPr !== undefined ? `<w:pPr>${pPr}</w:pPr>` : ''}${rPr !== undefined ? `<w:rPr>${rPr}</w:rPr>` : ''}</w:style>`);
  return xml.replace(match[0], style);
}

function patchStyles(xml) {
  xml = xml.replace(/<w:docDefaults>[\s\S]*?<\/w:docDefaults>/,
    `<w:docDefaults><w:rPrDefault><w:rPr>${fonts(font)}${color(ink)}${size(21)}`
    + `<w:lang w:val="en-GB" w:eastAsia="zh-CN" w:bidi="ar-SA"/></w:rPr></w:rPrDefault>`
    + `<w:pPrDefault><w:pPr><w:spacing w:after="120" w:line="276" w:lineRule="auto"/></w:pPr></w:pPrDefault></w:docDefaults>`);

  xml = restyle(xml, 'BodyText', { pPr: '<w:spacing w:before="100" w:after="100"/>' });
  xml = restyle(xml, 'Compact', { pPr: '<w:spacing w:before="20" w:after="40"/>' });

  // Title page.
  xml = restyle(xml, 'Title', { pPr: '<w:spacing w:before="3600" w:after="240"/><w:jc w:val="center"/>', rPr: `${fonts(font)}<w:b/>${color(navy)}<w:spacing w:val="20"/>${size(80)}` });
  xml = restyle(xml, 'TitleChar', { rPr: `${fonts(font)}<w:b/>${color(navy)}${size(80)}` });
  xml = restyle(xml, 'Subtitle', { pPr: '<w:spacing w:before="240" w:after="1600" w:line="300" w:lineRule="auto"/><w:ind w:left="720" w:right="720"/><w:jc w:val="center"/>', rPr: `${fonts(font)}<w:b w:val="0"/>${color(slate)}<w:spacing w:val="0"/>${size(28)}` });
  xml = restyle(xml, 'SubtitleChar', { rPr: `${color(slate)}${size(28)}` });
  xml = restyle(xml, 'Author', { pPr: '<w:keepNext/><w:spacing w:before="0" w:after="80"/><w:jc w:val="center"/>', rPr: `<w:b/>${color(ink)}${size(28)}` });
  xml = restyle(xml, 'Date', { pPr: '<w:keepNext/><w:spacing w:before="0" w:after="80"/><w:jc w:val="center"/>', rPr: `<w:b w:val="0"/>${color('5D6B7A')}${size(24)}` });

  // Headings: every chapter starts a page.
  const heading = (level, { before, after, sz, extraP = '' }) => restyle(xml, `Heading${level}`, {
    pPr: `<w:keepNext/><w:keepLines/>${extraP}<w:spacing w:before="${before}" w:after="${after}"/><w:outlineLvl w:val="${level - 1}"/>`,
    rPr: `${fonts(font)}<w:b/>${color(navy)}${size(sz)}`,
  });
  xml = heading(1, { before: 0, after: 360, sz: 40, extraP: `<w:pageBreakBefore/><w:pBdr>${border('bottom', navy, 12, 6)}</w:pBdr>` });
  xml = heading(2, { before: 360, after: 120, sz: 30 });
  xml = heading(3, { before: 280, after: 80, sz: 24 });
  xml = heading(4, { before: 200, after: 60, sz: 22 });
  for (const level of [1, 2, 3, 4]) {
    xml = restyle(xml, `Heading${level}Char`, { rPr: `${fonts(font)}<w:b/>${color(navy)}${size([40, 30, 24, 22][level - 1])}` });
  }
  xml = restyle(xml, 'TOCHeading', { pPr: '<w:pageBreakBefore/><w:spacing w:before="0" w:after="360"/><w:outlineLvl w:val="9"/>', rPr: `${fonts(font)}<w:b/>${color(navy)}${size(40)}` });

  // Quotes, links, inline code, tables.
  xml = restyle(xml, 'BlockText', { pPr: `<w:pBdr>${border('left', link, 24, 8)}</w:pBdr>${shade('F1F5FC')}<w:spacing w:before="80" w:after="80"/><w:ind w:left="240" w:right="240"/>`, rPr: color('263445') });
  xml = restyle(xml, 'Hyperlink', { rPr: color(link) });
  xml = restyle(xml, 'VerbatimChar', { rPr: `${fonts(mono)}${color('24292F')}${size(18)}${shade('EEF1F5')}` });
  xml = xml.replace(/(<w:style w:type="table" w:default="1" w:styleId="Table">[\s\S]*?<w:tblInd [^>]*\/>)/,
    `$1<w:tblBorders>${['top', 'left', 'bottom', 'right', 'insideH', 'insideV'].map((s) => border(s, rule, 4, 0)).join('')}</w:tblBorders>`);
  xml = xml.replace(/<w:tblStylePr w:type="firstRow">[\s\S]*?<\/w:tblStylePr>/,
    `<w:tblStylePr w:type="firstRow"><w:rPr><w:b/></w:rPr><w:tblPr/><w:trPr><w:tblHeader/></w:trPr><w:tcPr>${shade('EEF1F5')}<w:vAlign w:val="bottom"/></w:tcPr></w:tblStylePr>`);
  xml = xml.replace(/(<w:style w:type="table" w:default="1" w:styleId="Table">[\s\S]*?)(<w:tblPr>)/,
    `$1<w:pPr><w:spacing w:before="40" w:after="40" w:line="240" w:lineRule="auto"/></w:pPr><w:rPr>${size(18)}</w:rPr>$2`);

  // Code: one shaded, bordered block per listing; Word wraps long lines.
  const codeBorders = ['top', 'left', 'bottom', 'right'].map((s) => border(s, rule, 4, 4)).join('');
  const custom = [
    `<w:style w:type="paragraph" w:customStyle="1" w:styleId="SourceCode"><w:name w:val="Source Code"/><w:basedOn w:val="Normal"/><w:link w:val="VerbatimChar"/>`
      + `<w:pPr><w:pBdr>${codeBorders}</w:pBdr>${shade('F6F8FA')}<w:wordWrap w:val="1"/><w:spacing w:before="0" w:after="200" w:line="240" w:lineRule="auto"/><w:ind w:left="80" w:right="80"/></w:pPr>`
      + `<w:rPr>${fonts(mono)}${size(16)}</w:rPr></w:style>`,
    paragraphStyle('ListingPath', 'Listing Path', {
      basedOn: 'Normal', next: 'SourceCode',
      pPr: `<w:keepNext/><w:pBdr>${['top', 'left', 'bottom', 'right'].map((s) => border(s, navy, 4, 4)).join('')}</w:pBdr>${shade(navy)}<w:spacing w:before="200" w:after="0"/><w:ind w:left="80" w:right="80"/>`,
      rPr: `${fonts(mono)}<w:b/>${color('FFFFFF')}${size(16)}`,
    }),
    ...[['Tip', '1E7B45', 'E7F5EC'], ['Warning', 'B26A00', 'FFF4E0'], ['Exam', '6941C6', 'F3EEFC']].map(([id, edge, fill]) => paragraphStyle(id, id, {
      pPr: `<w:keepLines/><w:pBdr>${border('left', edge, 36, 8)}</w:pBdr>${shade(fill)}<w:spacing w:before="120" w:after="120"/><w:ind w:left="240" w:right="120"/>`,
    })),
    // Contents entries (build.mjs pre-fills them; Word refreshes them with page numbers).
    paragraphStyle('TOC1', 'toc 1', { basedOn: 'Normal', next: 'Normal', pPr: '<w:spacing w:before="120" w:after="20"/>', rPr: '<w:b/>' }),
    paragraphStyle('TOC2', 'toc 2', { basedOn: 'Normal', next: 'Normal', pPr: '<w:spacing w:before="0" w:after="20"/><w:ind w:left="360"/>', rPr: size(20) }),
  ].join('');
  return xml.replace('</w:styles>', `${custom}</w:styles>`);
}

const footerXml = `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:ftr xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><w:p><w:pPr><w:jc w:val="center"/></w:pPr><w:r><w:rPr>${color('5D6B7A')}${size(18)}</w:rPr><w:fldChar w:fldCharType="begin"/></w:r><w:r><w:rPr>${color('5D6B7A')}${size(18)}</w:rPr><w:instrText xml:space="preserve"> PAGE </w:instrText></w:r><w:r><w:rPr>${color('5D6B7A')}${size(18)}</w:rPr><w:fldChar w:fldCharType="separate"/></w:r><w:r><w:rPr>${color('5D6B7A')}${size(18)}</w:rPr><w:t>1</w:t></w:r><w:r><w:rPr>${color('5D6B7A')}${size(18)}</w:rPr><w:fldChar w:fldCharType="end"/></w:r></w:p></w:ftr>`;

// A4, 2 cm margins, a centred page number, none on the title page.
const sectPr = `<w:sectPr><w:footerReference w:type="default" r:id="rIdSentinelFooter"/>`
  + `<w:footnotePr><w:numRestart w:val="eachSect"/></w:footnotePr>`
  + `<w:pgSz w:w="11906" w:h="16838"/><w:pgMar w:top="1134" w:right="1134" w:bottom="1134" w:left="1134" w:header="567" w:footer="567" w:gutter="0"/>`
  + `<w:titlePg/></w:sectPr>`;

const source = execFileSync('pandoc', ['--print-default-data-file', 'reference.docx']);
const entries = readZip(source);
const text = (name) => entries.get(name).toString('utf8');

entries.set('word/styles.xml', Buffer.from(patchStyles(text('word/styles.xml')), 'utf8'));
entries.set('word/footer1.xml', Buffer.from(footerXml, 'utf8'));

let documentXml = text('word/document.xml').replace(/<w:sectPr>[\s\S]*?<\/w:sectPr>/, sectPr);
if (!documentXml.includes('xmlns:r=')) {
  documentXml = documentXml.replace('<w:document ', '<w:document xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" ');
}
entries.set('word/document.xml', Buffer.from(documentXml, 'utf8'));

entries.set('word/_rels/document.xml.rels', Buffer.from(text('word/_rels/document.xml.rels').replace('</Relationships>',
  '<Relationship Id="rIdSentinelFooter" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/footer" Target="footer1.xml"/></Relationships>'), 'utf8'));
entries.set('[Content_Types].xml', Buffer.from(text('[Content_Types].xml').replace('</Types>',
  '<Override PartName="/word/footer1.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.footer+xml"/></Types>'), 'utf8'));

writeFileSync(output, writeZip(entries));
console.log(`reference docx: ${output}`);
