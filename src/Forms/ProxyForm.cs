using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SDL2;
using System;
using System.Collections.Generic;

namespace XnaToFna.Forms {
    public sealed class ProxyForm : ProxyControl {

        private bool Dirty = false;
        // Don't make any initial changes coming from SDL overwrite the game's configuration.
        private bool WasEverDirty = false;

        public Rectangle _Bounds = new Rectangle();
        public override System.Drawing.Rectangle Bounds {
            get {
                return new System.Drawing.Rectangle(
                    _Bounds.X,
                    _Bounds.Y,
                    _Bounds.Width,
                    _Bounds.Height
                );
            }
            set {
                SetBounds(
                    value.X,
                    value.Y,
                    value.Width,
                    value.Height
                );
            }
        }
        public Rectangle SDLBounds = new Rectangle();

        private FormBorderStyle _FormBorderStyle = FormBorderStyle.FixedDialog;
        public FormBorderStyle FormBorderStyle {
            get {
                return _FormBorderStyle;
            }
            set {
                _FormBorderStyle = value;
                Dirty = true;
                WasEverDirty = true;
            }
        }

        private FormWindowState _WindowState = FormWindowState.Normal;
        public FormWindowState WindowState {
            get {
                return _WindowState;
            }
            set {
                _WindowState = value;
                Dirty = true;
                WasEverDirty = true;
            }
        }

        public void SDLWindowSizeChanged(object sender, EventArgs e) {
            if (!WasEverDirty) return;

            XnaToFnaGame game = XnaToFnaHelper.Game;
            IntPtr window = game.Window.Handle;

            SDLBounds = game.Window.ClientBounds;
            // SetBounds(SDLBounds.X, SDLBounds.Y, SDLBounds.Width, SDLBounds.Height);

            uint flags = SDL.SDL_GetWindowFlags(window);

            // Sidenote: "Fullscreen" means "the window is normal".

            /*
            if ((flags & (uint) SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN) != 0)
                _WindowState = FormWindowState.Normal;
            else if ((flags & (uint) SDL.SDL_WindowFlags.SDL_WINDOW_MAXIMIZED) != 0)
                _WindowState = FormWindowState.Maximized;
            else if ((flags & (uint) SDL.SDL_WindowFlags.SDL_WINDOW_MINIMIZED) != 0)
                _WindowState = FormWindowState.Minimized;
            else
                _WindowState = FormWindowState.Normal;

            if ((flags & (uint) (SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN | SDL.SDL_WindowFlags.SDL_WINDOW_BORDERLESS)) != 0)
                _FormBorderStyle = game.Window.AllowUserResizing ? FormBorderStyle.Sizable : FormBorderStyle.FixedDialog;
            */
        }

        public void SDLWindowChanged(
            IntPtr window,
            int clientWidth,
            int clientHeight,
            bool wantsFullscreen,
            string screenDeviceName,
            ref string resultDeviceName
        ) {
            SDLWindowSizeChanged(null, null);

            // Change anything on the form that could / should be updated

        }

        /// <summary>
        /// OnStyleChanged gets fired with every change each, not "once" at the end.
        /// ApplyChanges gets called by XnaToFnaGame.EndDraw to change this.
        /// </summary>
        public void ApplyChanges() {
            if (!Dirty)
                return;
            Dirty = false;

            XnaToFnaGame game = XnaToFnaHelper.Game;
            GraphicsDeviceManager gdm = XnaToFnaHelper.GetService<IGraphicsDeviceManager, GraphicsDeviceManager>();
            IntPtr window = game.Window.Handle;

            bool fullscreen = gdm?.IsFullScreen ?? false;

            bool borderless = FormBorderStyle == FormBorderStyle.None;
            bool maximized = WindowState == FormWindowState.Maximized;

            XnaToFnaHelper.Log("[ProxyForm] Applying changes from ProxyForm to SDL window");
            XnaToFnaHelper.Log($"[ProxyForm] Fullscreen: {gdm?.IsFullScreen ?? false}; Border: {FormBorderStyle}; State: {WindowState}");

            if (fullscreen)
                SDL.SDL_SetWindowFullscreen(window, (uint) SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN);
            else if (maximized && borderless)
                SDL.SDL_SetWindowFullscreen(window, (uint) SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP);
            else {
                SDL.SDL_SetWindowFullscreen(window, !fullscreen ? 0 : (uint) SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN);

                game.Window.IsBorderlessEXT = borderless;
                game.Window.AllowUserResizing = FormBorderStyle == FormBorderStyle.Sizable;

                if (maximized)
                    SDL.SDL_MaximizeWindow(window);
                else
                    SDL.SDL_RestoreWindow(window);
            }

            if (!fullscreen) {
                int x, y, w, h;
                SDL.SDL_GetWindowPosition(window, out x, out y);
                SDL.SDL_GetWindowSize(window, out w, out h);
                _Bounds = new Rectangle(x, y, w, h);
            }

        }

        public void SetBounds(int x, int y, int w, int h) {
            _Bounds = new Rectangle(x, y, w, h);

            // Update the SDL window
            IntPtr window = XnaToFnaHelper.Game.Window.Handle;
            SDL.SDL_GetWindowPosition(window, out x, out y);
            SDL.SDL_GetWindowSize(window, out w, out h);
            SDLBounds = _Bounds;
        }
        

    }

}
