-- Pandoc filter for the Word edition. It maps the book's HTML-only constructs
-- onto the paragraph styles defined in assets/reference.docx.
--
--   <div class="listing-path">   -> "Listing Path" (the dark label above a listing)
--   ::: tip / warning / exam     -> "Tip" / "Warning" / "Exam", with the label
--                                   the CSS adds via ::before written out in bold

local labels = { tip = 'Tip — ', warning = 'Watch out — ', exam = 'On the test — ' }
local styles = { tip = 'Tip', warning = 'Warning', exam = 'Exam' }

function Div(div)
  if div.classes:includes('listing-path') then
    div.attributes['custom-style'] = 'Listing Path'
    return div
  end

  for class, style in pairs(styles) do
    if div.classes:includes(class) then
      local first = div.content[1]
      if first and (first.t == 'Para' or first.t == 'Plain') then
        first.content:insert(1, pandoc.Strong { pandoc.Str(labels[class]) })
      end
      div.attributes['custom-style'] = style
      return div
    end
  end
end
