# Inserting Images and Resizing them
___

Relanota allows you to insert pictures in your notes, to better illustrate a concept.
There are two ways to insert these images into your notes: From the internet, a file on your computer, or the clipboard.

## Inserting Images from The Internet

You have found this really cool illustration on the internet, and want to include it in your notes.
That's great! Relanota supports inserting images from the internet, using standard Markdown syntax.
As such, `![Some ruby code](https://images.pexels.com/photos/546819/pexels-photo-546819.jpeg)` will insert an image of some Ruby code.
This is illustrated in the image below.

![web images](ms-appx:///Assets/web_image.png =700)

## Inserting Images from A File

You have a sweet image saved on your computer, of that one equation you were told to remember ages ago. Nice!
You can insert such images into Relanota, really easy.
Start by clicking the images button (Shortcut: `CTRL + I`) in the bottom of the note editor, or in cases where the window is too small, in the expandable menu.

![images button](ms-appx:///Assets/button_image.png =900)

By adding an image to your note this way, the image will be stored locally in Relanota, in the directory located at `{{local_dir}}`.
This is to persist the image, even if you delete it from it's original location.

## Inserting Images from Clipboard

You've just taken captured a screenshot from a screencast or poweroint, or copied an image from the internet, and want to insert it into your notes.
Move the caret (The text pointer) to the position where you wish to insert the image, and simply press `CTRL + V`.
This will insert the image where you want it to be, using markdown, as shown below.

![pasting images](ms-appx:///Assets/image_paste.png =900)

If you wish to move the image later, move the line where the image is has been pasted.

Furthermore, it is possible to resize images, by appending `=SIZE` to the file name or url to images as shown above.