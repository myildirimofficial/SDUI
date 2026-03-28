namespace Orivy;

/// <summary>
/// Specifies how an image is positioned within a PictureBox control.
/// Mirrors common PictureBox size mode options.
/// </summary>
public enum PictureBoxSizeMode
{
    /// <summary>Image is placed in the top-left corner of the control. The image is clipped if it is larger than the PictureBox.
    /// </summary>
    Normal,

    /// <summary>The image within the PictureBox is stretched or shrunk to fit the size of the PictureBox.</summary>
    StretchImage,

    /// <summary>The PictureBox is sized equal to the size of the image that it contains.</summary>
    AutoSize,

    /// <summary>The image is displayed in the center if the PictureBox is larger than the image; otherwise the image is placed in the top-left corner.</summary>
    CenterImage,

    /// <summary>The image is sized proportionally (with its original aspect ratio) to fit the PictureBox.</summary>
    Zoom
}