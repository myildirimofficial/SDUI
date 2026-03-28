namespace Orivy;

public delegate void MouseEventHandler(object sender, MouseEventArgs e);
public delegate void MouseWheelEventHandler(object sender, MouseWheelEventArgs e);
public delegate void MouseButtonEventHandler(object sender, MouseButtonEventArgs e);
public delegate void MouseMoveEventHandler(object sender, MouseMoveEventArgs e);
public delegate void MouseEnterEventHandler(object sender, MouseEventArgs e);
public delegate void MouseLeaveEventHandler(object sender, MouseEventArgs e);
public delegate void MouseClickEventHandler(object sender, MouseClickEventArgs e);
public delegate void MouseDoubleClickEventHandler(object sender, MouseDoubleClickEventArgs e);
public delegate void MouseDragEventHandler(object sender, MouseDragEventArgs e);
public delegate void MouseDropEventHandler(object sender, MouseDropEventArgs e);
public delegate void MouseHoverEventHandler(object sender, MouseHoverEventArgs e);

public delegate void KeyEventHandler(object sender, KeyEventArgs e);
public delegate void KeyPressEventHandler(object sender, KeyEventArgs e);