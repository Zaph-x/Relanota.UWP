# Math Mode
____

Relanote supports LaTeX flavoured math mode. In order to enter mathmode, start by entering a single dollar symbol `` $ `` and write your mathematical expression.
When you want to exit math mode, enter a single dollar symbol again.

**Note:** This action requires an internet connection the first time an expression is entered.

## LaTeX Math Equation Editor

If you are unfamiliar with LaTeX math syntax, we recommend looking at the [Codecogs LaTeX Equation Editor](https://latex.codecogs.com/eqneditor/editor.php), which is an online interactive equation editor.
Here you can get a list of all supported commands, as well as generate equations, which can be pasted directly into Relanote.

## Example

`$P(B) = \sum_a P(A,B)$`

Becomes

![math](ms-appx:///Assets/B8C7486DF1E3C29AA3D0628260EF6C2E.jpg)

## Technicalities

When a mathematical expression is entered for the first time, Relanote will download the image for that expressions, and store it on the disk. `` ({{cache_dir}}) ``

The application stores the image on disk, to fetch it faster next time the image has to be loaded, e.g. when restarting the application.