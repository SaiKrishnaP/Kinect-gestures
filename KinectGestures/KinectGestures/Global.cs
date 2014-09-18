using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Windows;

namespace KinectGestures
{
   public static class Global
    {
       public static void SendKey(UIElement sourceElement, Key keyToSend)
       {
           KeyEventArgs args = new KeyEventArgs(InputManager.Current.PrimaryKeyboardDevice, PresentationSource.FromVisual(sourceElement), 0, keyToSend);
           args.RoutedEvent = Keyboard.KeyDownEvent;
           InputManager.Current.ProcessInput(args);
       }
    }
}
