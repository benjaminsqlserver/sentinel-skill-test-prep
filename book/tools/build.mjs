// Builds the ebook from book/chapters/*.md.
//
//   node book/tools/build.mjs            -> book/dist/*.html, *.pdf, *.epub, *.docx
//   node book/tools/build.mjs --md-only  -> just the expanded markdown in book/build/
//
// Chapters never paste code by hand. They say
//
//   <!-- include: src/Sentinel.Domain/Common/Result.cs -->
//   <!-- include: src/Sentinel.Domain/Access/PageAccessEvaluator.cs#L60-L95 -->
//
// and this script replaces the comment with the file (or the line range) as a
// fenced listing headed by its path. The listing in the book is therefore
// always the code in the repository, and a renamed file breaks the build
// instead of silently going stale.

import { execFileSync } from 'node:child_process';
import { existsSync, mkdirSync, readdirSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, extname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const bookDir = resolve(here, '..');
const repoRoot = resolve(bookDir, '..');
const chaptersDir = join(bookDir, 'chapters');
const buildDir = join(bookDir, 'build');
const distDir = join(bookDir, 'dist');
const baseName = 'Sentinel-Skill-Test-Prep';

const languageByExtension = {
  '.cs': 'csharp', '.csproj': 'xml', '.props': 'xml', '.slnx': 'xml', '.json': 'json',
  '.ts': 'typescript', '.html': 'html', '.css': 'css', '.md': 'markdown', '.http': 'http',
  '.ps1': 'powershell', '.sh': 'bash', '.mjs': 'javascript', '.js': 'javascript', '.sql': 'sql',
  '.yml': 'yaml', '.yaml': 'yaml', '.txt': 'text',
};

const includePattern = /^<!--\s*include:\s*(?<path>[^#\s]+)(?:#L(?<from>\d+)-L(?<to>\d+))?\s*-->$/gm;

function expandIncludes(markdown, chapterFile) {
  return markdown.replace(includePattern, (_match, ...args) => {
    const { path, from, to } = args.at(-1);
    const absolute = join(repoRoot, path);
    if (!existsSync(absolute)) {
      throw new Error(`${chapterFile}: include not found: ${path}`);
    }

    let lines = readFileSync(absolute, 'utf8').replace(/\r\n/g, '\n').replace(/\n$/, '').split('\n');
    let label = path;
    if (from && to) {
      const start = Number(from);
      const end = Number(to);
      if (start < 1 || end > lines.length || start > end) {
        throw new Error(`${chapterFile}: ${path} has ${lines.length} lines; L${start}-L${end} is out of range`);
      }
      lines = lines.slice(start - 1, end);
      label = `${path} (lines ${start}–${end})`;
    }

    const language = languageByExtension[extname(path)] ?? 'text';
    // A fence longer than any run of backticks inside the file.
    const longestRun = Math.max(2, ...[...lines.join('\n').matchAll(/`+/g)].map((m) => m[0].length));
    const fence = '`'.repeat(longestRun + 1);
    return `<div class="listing-path">${label}</div>\n\n${fence}${language}\n${lines.join('\n')}\n${fence}`;
  });
}

function buildMarkdown() {
  const chapterFiles = readdirSync(chaptersDir).filter((f) => f.endsWith('.md')).sort();
  const parts = chapterFiles.map((file) => {
    const text = readFileSync(join(chaptersDir, file), 'utf8').replace(/\r\n/g, '\n');
    return expandIncludes(text, file);
  });

  mkdirSync(buildDir, { recursive: true });
  const output = join(buildDir, `${baseName}.md`);
  writeFileSync(output, parts.join('\n\n'), 'utf8');

  const words = parts.join(' ').split(/\s+/).length;
  console.log(`markdown: ${chapterFiles.length} files, ~${words.toLocaleString()} words -> ${output}`);
  return output;
}

function pandoc(args) {
  execFileSync('pandoc', args, { stdio: 'inherit', cwd: bookDir });
}

function findChrome() {
  const candidates = [
    process.env.CHROME_PATH,
    'C:/Program Files/Google/Chrome/Application/chrome.exe',
    'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',
    '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome',
    '/usr/bin/google-chrome',
    '/usr/bin/chromium',
  ].filter(Boolean);
  return candidates.find((c) => existsSync(c));
}

function buildOutputs(markdownFile) {
  mkdirSync(distDir, { recursive: true });
  const metadata = join(bookDir, 'metadata.yaml');
  const css = join(bookDir, 'assets', 'book.css');
  const html = join(distDir, `${baseName}.html`);

  const common = [markdownFile, '--metadata-file', metadata, '--toc', '--toc-depth=2', '--syntax-highlighting=tango'];

  pandoc([...common, '-s', '--embed-resources', '--css', css, '-o', html]);
  console.log(`html: ${html}`);

  pandoc([...common, '--css', css, '--split-level=1', '-o', join(distDir, `${baseName}.epub`)]);
  console.log(`epub: ${join(distDir, `${baseName}.epub`)}`);

  pandoc([...common, '-o', join(distDir, `${baseName}.docx`)]);
  console.log(`docx: ${join(distDir, `${baseName}.docx`)}`);

  const chrome = findChrome();
  if (!chrome) {
    console.warn('pdf: skipped (no Chrome or Edge found; set CHROME_PATH)');
    return;
  }
  const pdf = join(distDir, `${baseName}.pdf`);
  execFileSync(chrome, [
    '--headless=new', '--disable-gpu', '--no-pdf-header-footer',
    `--print-to-pdf=${pdf}`, `file:///${html.replace(/\\/g, '/')}`,
  ], { stdio: 'ignore' });
  console.log(`pdf: ${pdf}`);
}

const markdownFile = buildMarkdown();
if (!process.argv.includes('--md-only')) {
  buildOutputs(markdownFile);
}
