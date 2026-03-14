using System;
using System.Collections.Generic;
using System.ComponentModel;
using SkiaSharp;

namespace SDUI.Controls;

public class MenuItem
{
    private bool _autoSize = true;
    private SKColor _backColor = SKColor.Empty;
    private bool _checkOnClick;

    private CheckState _checkState = CheckState.Unchecked;
    private bool _enabled = true;
    private SKFont? _font;
    private SKColor _foreColor = SKColor.Empty;
    private SKBitmap _icon;
    private SKImage _image;
    private SKColor _imageTransparentColor = SKColor.Empty;
    private bool _isSeparator;
    private string _name = string.Empty;
    private Thickness _padding = new(3);
    private Keys _shortcutKeys = Keys.None;
    private bool _showSubmenuArrow = true;
    private SKSize _size;
    private string _text = string.Empty;
    private ContentAlignment _textAlign = ContentAlignment.MiddleLeft;
    private bool _visible = true;

    public MenuItem(string text = "", SKBitmap icon = null)
    {
        _text = text;
        _icon = icon;
        UpdateSize();
    }

    public object Tag { get; set; }

    /// <summary>
    ///     TODO!
    /// </summary>
    public Aligment Alignment { get; set; } = Aligment.Left;

    [Category("Behavior")]
    [DefaultValue(false)]
    public bool CheckOnClick
    {
        get => _checkOnClick;
        set
        {
            if (_checkOnClick == value) return;
            _checkOnClick = value;
        }
    }

    [Category("Appearance")]
    [DefaultValue(typeof(CheckState), "Unchecked")]
    public CheckState CheckState
    {
        get => _checkState;
        set
        {
            if (_checkState == value) return;
            var oldChecked = _checkState == CheckState.Checked;
            _checkState = value;
            var newChecked = _checkState == CheckState.Checked;

            Parent?.Invalidate();

            if (oldChecked != newChecked) CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    [Category("Appearance")]
    [DefaultValue(false)]
    public bool Checked
    {
        get => _checkState == CheckState.Checked;
        set => CheckState = value ? CheckState.Checked : CheckState.Unchecked;
    }

    public string Text
    {
        get => _text;
        set
        {
            if (_text == value) return;
            _text = value;
            if (_autoSize) UpdateSize();
            Parent?.Invalidate();
        }
    }

    [Category("Design")]
    [DefaultValue("")]
    public string Name
    {
        get => _name;
        set
        {
            if (_name == value) return;
            _name = value;
        }
    }

    public SKBitmap Icon
    {
        get => _icon;
        set
        {
            if (_icon == value) return;
            _icon = value;
            if (_autoSize) UpdateSize();
            Parent?.Invalidate();
        }
    }

    [Category("Behavior")]
    [DefaultValue(typeof(Keys), "None")]
    public Keys ShortcutKeys
    {
        get => _shortcutKeys;
        set
        {
            if (_shortcutKeys == value) return;
            _shortcutKeys = value;
            if (_autoSize) UpdateSize();
            Parent?.Invalidate();
        }
    }

    [Category("Appearance")]
    public SKImage Image
    {
        get => _image;
        set
        {
            if (_image == value) return;
            _image = value;
            if (_autoSize) UpdateSize();
            Parent?.Invalidate();
        }
    }

    [Category("Appearance")]
    [DefaultValue(typeof(SKColor), "Empty")]
    public SKColor ImageTransparentColor
    {
        get => _imageTransparentColor;
        set
        {
            if (_imageTransparentColor == value) return;
            _imageTransparentColor = value;
            Parent?.Invalidate();
        }
    }

    [Category("Behavior")]
    [DefaultValue(true)]
    public bool ShowSubmenuArrow
    {
        get => _showSubmenuArrow;
        set
        {
            if (_showSubmenuArrow == value) return;
            _showSubmenuArrow = value;
            if (_autoSize) UpdateSize();
            Parent?.Invalidate();
        }
    }

    public bool IsSeparator
    {
        get => _isSeparator;
        set
        {
            if (_isSeparator == value) return;
            _isSeparator = value;
            Parent?.Invalidate();
        }
    }

    [Category("Layout")]
    public SKSize Size
    {
        get => _size;
        set
        {
            if (_size == value) return;
            _size = value;
            _autoSize = false;
            Parent?.Invalidate();
        }
    }

    [Category("Appearance")]
    public SKColor ForeColor
    {
        get => _foreColor;
        set
        {
            if (_foreColor == value) return;
            _foreColor = value;
            Parent?.Invalidate();
        }
    }

    [Category("Appearance")]
    public SKColor BackColor
    {
        get => _backColor;
        set
        {
            if (_backColor == value) return;
            _backColor = value;
            Parent?.Invalidate();
        }
    }

    [Category("Behavior")]
    [DefaultValue(true)]
    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value) return;
            _enabled = value;
            Parent?.Invalidate();
        }
    }

    [Category("Behavior")]
    [DefaultValue(true)]
    public bool Visible
    {
        get => _visible;
        set
        {
            if (_visible == value) return;
            _visible = value;
            Parent?.Invalidate();
        }
    }

    [Category("Appearance")]
    [Browsable(false)]
    internal bool HasCustomFont => _font != null;

    [Category("Appearance")]
    [Browsable(false)]
    internal SKFont ResolvedFont => _font ?? Parent?.ResolvedFont ?? Application.SharedDefaultFont;

    [Category("Appearance")]
    public SKFont Font
    {
        get => ResolvedFont.CloneFont();
        set
        {
            if (_font.FontEquals(value)) return;
            _font?.Dispose();
            _font = value?.CloneFont();
            if (_autoSize) UpdateSize();
            Parent?.Invalidate();
        }
    }

    public void ResetFont()
    {
        if (_font == null)
            return;

        _font.Dispose();
        _font = null;
        if (_autoSize) UpdateSize();
        Parent?.Invalidate();
    }

    [Category("Appearance")]
    [DefaultValue(ContentAlignment.MiddleLeft)]
    public ContentAlignment TextAlign
    {
        get => _textAlign;
        set
        {
            if (_textAlign == value) return;
            _textAlign = value;
            Parent?.Invalidate();
        }
    }

    [Category("Layout")]
    public Thickness Padding
    {
        get => _padding;
        set
        {
            if (_padding == value) return;
            _padding = value;
            if (_autoSize) UpdateSize();
            Parent?.Invalidate();
        }
    }

    [Category("Behavior")]
    [DefaultValue(true)]
    public bool AutoSize
    {
        get => _autoSize;
        set
        {
            if (_autoSize == value) return;
            _autoSize = value;
            if (value) UpdateSize();
            Parent?.Invalidate();
        }
    }

    public bool HasDropDown => DropDownItems.Count > 0;
    public List<MenuItem> DropDownItems { get; } = new();

    internal MenuStrip Parent { get; set; }

    private void UpdateSize()
    {
        if (Parent == null) return;

        var font = ResolvedFont;
        using var scaledFont = new SKFont(font.Typeface ?? SKTypeface.Default)
        {
            Size = font.Size.Topx(Parent),
            Subpixel = true,
            Edging = SKFontEdging.SubpixelAntialias,
            Hinting = SKFontHinting.Full
        };

        var textBounds = new SkiaSharp.SKRect();
        scaledFont.MeasureText(Text, out textBounds);

        var width = (int)textBounds.Width + Padding.Horizontal;
        var height = (int)textBounds.Height + Padding.Vertical;

        if (Icon != null || Image != null)
        {
            var imageWidth = Icon?.Width ?? Image?.Width ?? 0;
            var imageHeight = Icon?.Height ?? Image?.Height ?? 0;
            width += imageWidth + 4;
            height = Math.Max(height, imageHeight + Padding.Vertical);
        }

        if (HasDropDown && ShowSubmenuArrow) width += 12; // Ok işareti için ekstra genişlik

        if (ShortcutKeys != Keys.None)
        {
            var shortcutText = ShortcutKeys.ToString();
            var shortcutBounds = new SkiaSharp.SKRect();
            scaledFont.MeasureText(shortcutText, out shortcutBounds);
            width += (int)shortcutBounds.Width + 20; // Kısayol tuşu için ekstra genişlik
        }

        _size = new SKSize(width, height);
    }

    public static MenuItem CreateSeparator()
    {
        return new MenuItemSeparator();
    }

    public void AddDropDownItem(MenuItem item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        DropDownItems.Add(item);
        Parent?.Invalidate();
    }

    public void RemoveDropDownItem(MenuItem item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        if (DropDownItems.Remove(item)) Parent?.Invalidate();
    }

    internal void OnClick()
    {
        if (!Enabled) return;

        if (CheckOnClick) CheckState = CheckState == CheckState.Checked ? CheckState.Unchecked : CheckState.Checked;

        Click?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler Click;
    public event EventHandler CheckedChanged;
}

public class MenuItemSeparator : MenuItem
{
    private float _height = 2f;
    private SKColor _lineColor = new SKColor(100, 100, 100);
    private float _margin = 4f;
    private SKColor _shadowColor = new SKColor(50, 50, 50);

    public MenuItemSeparator()
    {
        IsSeparator = true;
    }

    [Category("Appearance")]
    public float Height
    {
        get => _height;
        set
        {
            if (_height == value) return;
            _height = value;
            Parent?.Invalidate();
        }
    }

    [Category("Layout")]
    public float Margin
    {
        get => _margin;
        set
        {
            if (_margin == value) return;
            _margin = value;
            Parent?.Invalidate();
        }
    }

    [Category("Appearance")]
    public SKColor LineColor
    {
        get => _lineColor;
        set
        {
            if (_lineColor == value) return;
            _lineColor = value;
            Parent?.Invalidate();
        }
    }

    [Category("Appearance")]
    public SKColor ShadowColor
    {
        get => _shadowColor;
        set
        {
            if (_shadowColor == value) return;
            _shadowColor = value;
            Parent?.Invalidate();
        }
    }
}

public static class MenuStripExtensions
{
    public static MenuItem AddMenuItem(this MenuStrip menu, string text, EventHandler onClick = null,
        Keys shortcut = Keys.None)
    {
        var item = new MenuItem(text)
        {
            ShortcutKeys = shortcut
        };
        if (onClick != null)
            item.Click += onClick;
        menu.AddItem(item);
        return item;
    }

    public static MenuItem AddMenuItem(this MenuItem parent, string text, EventHandler onClick = null,
        Keys shortcut = Keys.None)
    {
        var item = new MenuItem(text)
        {
            ShortcutKeys = shortcut
        };
        if (onClick != null)
            item.Click += onClick;
        parent.AddDropDownItem(item);
        return item;
    }

    public static MenuItemSeparator AddSeparator(this MenuStrip menu)
    {
        var separator = new MenuItemSeparator();
        menu.AddItem(separator);
        return separator;
    }

    public static MenuItemSeparator AddSeparator(this MenuItem parent)
    {
        var separator = new MenuItemSeparator();
        parent.AddDropDownItem(separator);
        return separator;
    }
}