# Test with markdown

[[_TOC_]]

The table of contents is not supported. Should render just the tag in PDF.

## Guidance

This page implements most of the markdown examples given on the [DevOps wiki docs](https://docs.microsoft.com/en-us/azure/devops/project/wiki/markdown-guidance?view=azure-devops)

It is not claimed or stated that all these features should work. However, this a quite complete test for the PDF generation.

At some points the official docs show images instead of the rendered content. I suspect that this is to support multiple browsers and even the 'print to PDF' functionality in a browser.

## Headers

Structure your comments using headers. Headers segment longer comments, making them easier to read.

Start a line with a hash character `#` to set a heading. Organize your remarks with subheadings by starting a line with additional hash characters, for example `####`. Up to six levels of headings are supported.

**Example:**
```markdown
# This is a H1 header
## This is a H2 header
### This is a H3 header
#### This is a H4 header
##### This is a H5 header
```

**Result:**

# This a H1 header
## This a H2 header
### This a H3 header
#### This a H4 header
##### This a H5 header
     

## Paragraphs and line breaks


Make your text easier to read by breaking it up with paragraphs or line breaks.  

In pull request comments, select **Enter** to insert a line break, and begin text on a new line.

In a Markdown file or widget, enter two spaces before the line break to begin a new paragraph, or enter two consecutive line breaks to begin a new paragraph.


**Example - pull request comment:**

<pre>
Add lines between your text with the Enter key.
This spaces your text better and makes it easier to read.
</pre>

**Result:**
Add lines between your text with the Enter key.
This action spaces your text better and makes it easier to read.

**Example - Markdown file or widget:**

<pre>
Add two spaces before the end of the line.(space, space)
This adds space in between paragraphs.
</pre>

**Result:**  
Add two spaces before the end of the line.  
Space is added in between paragraphs.

## Blockquotes

Quote previous comments or text to set the context for your comment or text.

Quote single lines of text with `>` before the text. Use many `>` characters to nest quoted text.
Quote blocks of lines of text by using the same level of `>` across many lines.

**Example:**

<pre>
> Single line quote
>> Nested quote
>> multiple line
>> quote
</pre>

**Result:**  

> Single line quote
>> Nested quote  
>> multiple line  
>> quote

## Horizontal rules

To add a horizontal rule, add a line that's a series of dashes `---`. The line above the line containing the `---` must be blank.

**Example:**

<div id="do_not_render">
<pre>
above
&nbsp;
&#45;&#45;&#45;&#45;
below
</pre>
</div>

**Result:**  

above    

-----    

below    

## Emphasis (bold, italics, strikethrough) 

You can emphasize text by applying bold, italics, or strikethrough to characters:

- To apply italics: surround the text with an asterisk `*` or underscore `_` 
- To apply bold: surround the text with double asterisks `**`.
- To apply strikethrough: surround the text with double tilde characters `~~`.

Combine these elements to apply emphasis to text.


> There is no Markdown syntax that supports underlining text. Within a wiki page, you can use the HTML `<u>` tag to generate underlined text. For example, `<u>underlined text</u>` yields <u>underlined text</u>.


**Example:**

<pre>
Use _emphasis_ in comments to express **strong** opinions and point out ~~corrections~~  
**_Bold, italicized text_**  
**~~Bold, strike-through text~~**
</pre>

<br/>

**Result:**  

Use _emphasis_ in comments to express **strong** opinions and point out <s>corrections</s>  
**_Bold, italicized text_**
**~~Bold, strike-through text~~**  


## Code highlighting


Highlight suggested code segments using code highlight blocks.
To indicate a span of code, wrap it with three backtick quotes (<code>&#96;&#96;&#96;</code>) on a new line at both the start and end of the block. To indicate code inline, wrap it with one backtick quote (<code>&#96;</code>).

> Code highlighting entered within the Markdown widget renders code as plain preformatted text.


**Example:**

<pre>&#96;&#96;&#96;
sudo npm install vsoagent-installer -g  
&#96;&#96;&#96;
</pre>  

<br/>

**Result:**

```
sudo npm install vsoagent-installer -g
```

<br/>

**Example:**

<pre>
To install the Microsoft Cross Platform Build & Release Agent, run the following: &#96;$ sudo npm install vsoagent-installer -g&#96;.
</pre>

<br/>

**Result:**

To install the Microsoft Cross Platform Build & Release Agent, run the following command: `$ sudo npm install vsoagent-installer -g`.  

<br/>

Within a Markdown file, text with four spaces at the beginning of the line automatically converts to a code block.  

Set a language identifier for the code block to enable syntax highlighting for any of the supported languages in [highlightjs](https://github.com/highlightjs/highlight.js/tree/9.10.0/src/languages), version v9.10.0.

<pre>
``` language
code
```
</pre>

<br/>

**Additional examples:**

<pre>
``` js
const count = records.length;
```
</pre>

``` js
const count = records.length;
```

<br/>

<pre>
``` csharp
Console.WriteLine("Hello, World!");
```
</pre>

``` csharp
Console.WriteLine("Hello, World!");
```


## Tables


Organize structured data with tables. Tables are especially useful for describing function parameters, object methods, and other data that have
a clear name to description mapping. You can format tables in pull requests, wiki, and Markdown files such as README files and Markdown widgets.  

- Place each table row on its own line
- Separate table cells using the pipe character `|`
- The first two lines of a table set the column headers and the alignment of elements in the table
- Use colons (`:`) when dividing the header and body of tables to specify column alignment (left, center, right)
- To start a new line, use the HTML break tag (`<br/>`) (Works within a Wiki but not elsewhere)  
- Make sure to end each row with a CR or LF.
- A blank space is required before and after work item or pull request (PR) mentions inside a table cell.

**Example:**

```markdown
| Heading 1 | Heading 2 | Heading 3 |  
|-----------|:-----------:|-----------:|  
| Cell A1 | Cell A2 | Cell A3 |  
| Cell B1 | Cell B2 | Cell B3<br/>second line of text |  
```


**Result:**  

| Heading 1 | Heading 2 | Heading 3 |  
|-----------|:---------:|-----------:|  
| Cell A1 | Cell A2 | Cell A3 |  
| Cell B1 | Cell B2 | Cell B3<br/>second line of text |  

## Lists

Organize related items with lists. You can add ordered lists with numbers, or unordered lists with just bullets.

Ordered lists start with a number followed by a period for each list item. Unordered lists start with a `-`. Begin each list item on a new line. In a Markdown file or widget, enter two spaces before the line break to begin a new paragraph, or enter two line breaks consecutively to begin a new paragraph.

### Ordered or numbered lists

**Example:**

```markdown
1. First item.
1. Second item.
1. Third item.
```

**Result:**

1. First item.
2. Second item.
3. Third item.

### Bullet lists

**Example:**

```
- Item 1
- Item 2
- Item 3
```

**Result:**

- Item 1
- Item 2
- Item 3

### Nested lists

**Example:**

```
1. First item.
   - Item 1
   - Item 2
   - Item 3
1. Second item.
   - Nested item 1
   - Nested item 2
   - Nested item 3 
```

**Result:**  

1. First item.
    - Item 1
    - Item 2
    - Item 3
2. Second item.
    - Nested item 1
    - Nested item 2
    - Nested item 3


## Links

In pull request comments and wikis, HTTP and HTTPS URLs are automatically formatted as links. You can link to work items by entering the *#* key and a work item ID, and then choosing the work item from the list.

Avoid auto suggestions for work items by prefixing *#* with a backslash (`\`). This action can be useful if you want to use *#* for color hex codes.

In Markdown files and widgets, you can set text hyperlinks for your URL using the standard Markdown link syntax:

```markdown
[Link Text](Link URL)
```

When linking to another Markdown page in the same Git or TFVC repository, the link target can be a relative path or an absolute path in the repository.  

**Supported links for Welcome pages:**

- Relative path: `[text to display](/target.md)` 
- Absolute path in Git: `[text to display](/folder/target.md)`
- Absolute path in TFVC: `[text to display]($/project/folder/target.md)`
- URL: `[text to display](http://address.com)`

**Supported links for Markdown widget:**

<ul>
<li>URL: <code>[text to display](http://address.com)</code>  </li>
</ul>

**Supported links for Wiki:**  
<ul>
<li>Absolute path of Wiki pages: <code>[text to display](/parent-page/child-page)</code> </li>
<li>URL: <code>[text to display](http://address.com)</code>  </li>
</ul>



### Anchor links

Within Markdown files, anchor IDs are assigned to all headings when rendered as HTML. The ID is the heading text, with the spaces replaced by dashes (-) and all lower case. In general, the following conventions apply:

- Punctuation marks and leading white spaces within a file name are ignored
- Upper case letters are  converted to lower
- Spaces between letters are converted to dashes (-).

**Example:**

```
###Link to a heading in the page
```


**Result:**

The syntax for an anchor link to a section...

<pre>
[Link to a heading in the page](#link-to-a-heading-in-the-page)
</pre>
<br/>
The ID is all lower case, and the link is case-sensitive, so be sure to use lower case, even though the heading itself uses upper case.

You can also reference headings within another Markdown file:

<pre>
[text to display](./target.md#heading-id)  
</pre>

<br/>
In wiki, you can also reference heading in another page:

<pre>
[text to display](/page-name#section-name)
</pre>

<a name="images"> </a>

## Images


To highlight issues or make things more interesting, you can add images and animated GIFs to the following aspects in your pull requests:

- Comments
- Markdown files
- Wiki pages

Use the following syntax to add an image: <div id="do_not_render"><pre>&#33;&#91;Text](URL)</pre></div> The text in the brackets describes the image being linked and the URL points to the image location.

**Example:**

<pre>
![Illustration to use for new users](https://azurecomcdn.azureedge.net/cvt-779fa2985e70b1ef1c34d319b505f7b4417add09948df4c5b81db2a9bad966e5/images/page/services/devops/hero-images/index-hero.jpg)
</pre>



**Result:**

![Illustration of linked image](https://azurecomcdn.azureedge.net/cvt-779fa2985e70b1ef1c34d319b505f7b4417add09948df4c5b81db2a9bad966e5/images/page/services/devops/hero-images/index-hero.jpg)

The path to the image file can be a relative path or the absolute path in Git or TFVC, just like the path to another Markdown file in a link.  

- Relative path: `![Image alt text](./image.png)`  
- Absolute path in Git: `![Image alt text](/media/markdown-guidance/image.png)`  
- Absolute path in TFVC: `![Image alt text]($/project/folder/media/markdown-guidance/image.png)`  
- Resize image: `IMAGE_URL =WIDTHxHEIGHT`

> Be sure to include a space before the equal sign.
>

- Example: `![Image alt text]($/project/folder/media/markdown-guidance/image.png =500x250)`
- It's also possible to specify only the WIDTH by leaving out the HEIGHT value: `IMAGE_URL =WIDTHx`



## Checklist or task list


You can Use `[ ]` or `[x]` to support checklists. Precede the checklist with either `-<space>` or `1.<space>` (any numeral).


**Example - Format a list as a task list**

<pre>
- [ ] A  
- [ ] B  
- [ ] C  
- [x] A  
- [x] B  
- [x] C  
</pre>


**Result:**  

- [ ] A  
- [ ] B  
- [ ] C  
- [x] A  
- [x] B  
- [x] C  


> A checklist within a table cell isn't supported.


## Emoji

**Example:**

<pre>
:smile:
:angry:
</pre>


**Result:**  

:smile:
:angry:

To escape emojis, enclose them using the \` character.

**Example:**

<pre>`:smile:` `:)` `:angry:`</pre>

**Result:**

 `:smile:` `:)` `:angry:`

See a full list of emoji and how they render [on the Emoji page](./emoji.md#Full-set-of-Emoji)
(this is also an example of a link wiht an anchor to another pase)


## Ignore or escape Markdown syntax to enter specific or literal characters

<table width="650px">
<tbody valign="top">
<tr>
<th width="300px">Syntax</th>
<th width="350px">Example/notes</th>
</tr>
<tr>
<td>
<p>To insert one of the following characters, prefix with a <code>&#92;</code>(backslash).</p>
<p style="margin-bottom:2px;"><code>&#92;</code>, backslash </p>
<p style="margin-bottom:2px;"><code>&#96;</code>, backtick</p>
<p style="margin-bottom:2px;"><code>&#95;</code>, underscore</p>
<p style="margin-bottom:2px;"><code>{}</code>, curly braces </p>
<p style="margin-bottom:2px;"><code>[]</code>, square brackets</p>
<p style="margin-bottom:2px;"><code>()</code>, parentheses</p>
<p style="margin-bottom:2px;"><code>#</code>, hash mark </p>
<p style="margin-bottom:2px;"><code>+</code>, plus sign</p>
<p style="margin-bottom:2px;"><code>-</code>, minus sign (hyphen)</p>
<p style="margin-bottom:2px;"><code>.</code>, period </p>
<p style="margin-bottom:2px;"><code>!</code>, exclamation mark</p>
<p style="margin-bottom:2px;"><code>*</code>, asterisk</p>
</td>
<td>
<p>Some examples on inserting special characters:</p>

<p>Enter <code>&#92;&#92;</code> to get \ </p>
<p>Enter <code>&#92;&#95;</code> to get &#95; </p>
<p>Enter <code>&#92;# </code> to get # </p>
<p>Enter <code>&#92;(</code> to get ( </p> 
<p>Enter <code>&#92;.</code> to get . </p>
<p>Enter <code>&#92;!</code> to get ! </p>
<p>Enter <code>&#92;*</code> to get * </p>

</td>
</tr>
</tbody>
</table>


## Mathematical notation and characters

#### Supported in: Pull Requests | Wikis

Both inline and block [KaTeX](https://khan.github.io/KaTeX/function-support.html) notation is supported in wiki pages and pull requests. The following supported elements are included:

- Symbols
- Greek letters
- Mathematical operators
- Powers and indices
- Fractions and binomials
- Other KaTeX supported elements

To include mathematical notation, surround the mathematical notation with a `$` sign, for inline, and `$$` for block,  as shown in the following examples:


> This feature is supported within Wiki pages and pull requests for TFS 2018.2 or later versions.


### Example: Greek characters

```KaTeX
$
\alpha, \beta, \gamma, \delta, \epsilon, \zeta, \eta, \theta, \kappa, \lambda, \mu, \nu, \omicron, \pi, \rho, \sigma, \tau, \upsilon, \phi, ...
$  


$\Gamma,  \Delta,  \Theta, \Lambda, \Xi, \Pi, \Sigma, \Upsilon, \Phi, \Psi, \Omega$
```

**Result:**

$
\alpha, \beta, \gamma, \delta, \epsilon, \zeta, \eta, \theta, \kappa, \lambda, \mu, \nu, \omicron, \pi, \rho, \sigma, \tau, \upsilon, \phi, ...
$  


$\Gamma,  \Delta,  \Theta, \Lambda, \Xi, \Pi, \Sigma, \Upsilon, \Phi, \Psi, \Omega$

### Example: Algebraic notation

```KaTeX
Area of a circle is $\pi r^2$

And, the area of a triangle is:

$$
A_{triangle}=\frac{1}{2}({b}\cdot{h})
$$
```

**Result:**

Area of a circle is $\pi r^2$

And, the area of a triangle is:

$$
A_{triangle}=\frac{1}{2}({b}\cdot{h})
$$
### Example: Sums and Integrals

```KaTeX
$$
\sum_{i=1}^{10} t_i
$$


$$
\int_0^\infty \mathrm{e}^{-x}\,\mathrm{d}x
$$     
```

**Result:**

$$
\sum_{i=1}^{10} t_i
$$


$$
\int_0^\infty \mathrm{e}^{-x}\,\mathrm{d}x
$$ 

# Additional

I tried to recreate the [Microsoft Docs Alerts](https://docs.microsoft.com/en-us/contribute/markdown-reference#selectors)
They need additional styling. Might be best to somehow implement custom markdown tags. Then you can improve by adding HTML to get an example like this:
[Improved alerts](/learn)

This one needs a red background

| :x: **CAUTION**  |
|:----|
| There will be irreversible consequences|

This one needs a yellow background

| :warning: **WARNING**  |
|:----|
| Dangerous certain consequences of an action can apply|

This one needs a purlple background

| :grey_exclamation: **Important**  |
|:----|
| Essential information required for success |

This one needs a light blue background

| :bulb: **Tip**  |
|:----|
| Optional information to help the user be more successful!|

This one needs a green background

| :pencil2: **Note**  |
|:----|
| Information the user should note when skimming the document! |

## Resources

Emoji cheat sheet: https://www.webfx.com/tools/emoji-cheat-sheet/




