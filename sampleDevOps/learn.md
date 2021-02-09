# How to learn

Here are a few recources

* https://docs.microsoft.com/en-us/azure/devops/project/wiki/publish-repo-to-wiki?view=azure-devops&tabs=browser

## Markdown examples

## Text

**bold**, _italic_, ~~strickthrough~~, <u>underline using html</u>

> blockqote example

above a line

----
under the line

`Single line of code`

``` js
print("block of code");
```


|table| header1 | column |
|----|----|----|
|cell  |cell  |cell  |
|cell  |cell  |cell  |


## Mermaid diagram

::: mermaid
 graph LR;
 A[Wiki supports Mermaid] --> B[Visit https://mermaidjs.github.io/ for Mermaid syntax];
:::

## pointers

This one needs a red background

<div class="is-caution">

| :x: **CAUTION**  |
|:----|
| There will be irreversible consequences|
</div>

This one needs a yellow background

<div class="is-warning">

| :warning: **WARNING**  |
|:----|
| Dangerous certain consequences of an action can apply|
</div>

This one needs a purlple background

<div class="is-important">

| :grey_exclamation: **Important**  |
|:----|
| Essential information required for success |
</div>

This one needs a light blue background

<div class="is-tip">

| :bulb: **Tip**  |
|:----|
| Optional information to help the user be more successful!|
</div>

This one needs a green background

<div class="is-note">

| :pencil2: **Note**  |
|:----|
| Information the user should note when skimming the document! |
</div>

## Shorthand

Here is an example of how to render a PDF from the command line.

```
 .\azuredevops-export-wiki.exe -v --debug --css .\style.css --header-url .\header.html --footer-url .\footer.html
```


