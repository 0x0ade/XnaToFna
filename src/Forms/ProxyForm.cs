using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SDL2;
using System;
using System.Collections.Generic;

namespace XnaToFna.Forms {
    public sealed class ProxyForm : ProxyControl {

        private const uint SDL_WINDOW_FULLSCREEN_DESKTOP_ONLY = 0x00001000;

        private bool _Dirty = false;
        private bool Dirty {
            get {
                return _Dirty;
            }
            set {
                if (value) {
                    _FormBorderStyle = FormBorderStyle;
                    _WindowState = WindowState;
                }

                _Dirty = value;
            }
        }

        private bool FakeFullscreenWindow = false;

        public Rectangle WindowedBounds = new Rectangle();
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
                SDLBounds = _Bounds = WindowedBounds = new Rectangle(
                    value.X,
                    value.Y,
                    value.Width,
                    value.Height
                );
            }
        }

        public Rectangle SDLBounds {
            get {
                return XnaToFnaHelper.Game.Window.ClientBounds;
            }
            set {
                IntPtr window = XnaToFnaHelper.Game.Window.Handle;
                SDL.SDL_SetWindowSize(window, value.Width, value.Height);
                SDL.SDL_SetWindowPosition(window, value.X, value.Y);
            }
        }

        private FormBorderStyle _FormBorderStyle = FormBorderStyle.FixedDialog;
        public FormBorderStyle FormBorderStyle {
            get {
                if (Dirty)
                    return _FormBorderStyle;
                if (XnaToFnaHelper.Game.Window.IsBorderlessEXT || FakeFullscreenWindow)
                    return FormBorderStyle.None;
                if (XnaToFnaHelper.Game.Window.AllowUserResizing)
                    return FormBorderStyle.Sizable;
                return FormBorderStyle.FixedDialog;
            }
            set {
                Dirty = true;
                _FormBorderStyle = value;
            }
        }

        private FormWindowState _WindowState = FormWindowState.Normal;
        public FormWindowState WindowState {
            get {
                if (Dirty)
                    return _WindowState;
                uint flags = SDL.SDL_GetWindowFlags(XnaToFnaHelper.Game.Window.Handle);
                if ((flags & (uint) SDL.SDL_WindowFlags.SDL_WINDOW_MAXIMIZED) != 0 || FakeFullscreenWindow)
                    return FormWindowState.Maximized;
                if ((flags & (uint) SDL.SDL_WindowFlags.SDL_WINDOW_MINIMIZED) != 0)
                    return FormWindowState.Minimized;
                return FormWindowState.Normal;
            }
            set {
                Dirty = true;
                _WindowState = value;
            }
        }

        public void SDLWindowSizeChanged(object sender, EventArgs e) {
            Rectangle sdlBounds = SDLBounds;
            _Bounds = new Rectangle(sdlBounds.X, sdlBounds.Y, sdlBounds.Width, sdlBounds.Height);

            if ((SDL.SDL_GetWindowFlags(XnaToFnaHelper.Game.Window.Handle) & (uint) SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN) == 0 && !FakeFullscreenWindow)
                WindowedBounds = _Bounds;
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
        }

        /// <summary>
        /// OnStyleChanged gets fired with every change each, not "once" at the end.
        /// ApplyChanges gets called by XnaToFnaGame.EndDraw to change this.
        /// </summary>
        public void ApplyChanges() {
            if (!Dirty)
                return;

            XnaToFnaGame game = XnaToFnaHelper.Game;
            IntPtr window = game.Window.Handle;
            GraphicsDeviceManager gdm = XnaToFnaHelper.GetService<IGraphicsDeviceManager, GraphicsDeviceManager>();
            bool fullscreen = gdm.IsFullScreen;

            bool borderless = FormBorderStyle == FormBorderStyle.None;
            bool maximized = WindowState == FormWindowState.Maximized;
            bool wasFakeFullscreenWindow = FakeFullscreenWindow;
            FakeFullscreenWindow = maximized && borderless;

            XnaToFnaHelper.Log("[ProxyForm] Applying changes from ProxyForm to SDL window");
            XnaToFnaHelper.Log($"[ProxyForm] Currently fullscreen: {fullscreen}; Fake fullscreen window: {FakeFullscreenWindow}; Border: {FormBorderStyle}; State: {WindowState}");

            if (FakeFullscreenWindow) {
                XnaToFnaHelper.Log("[ProxyForm] Game expects borderless fullscreen... give it proper fullscreen instead.");

                if (!fullscreen)
                    WindowedBounds = SDLBounds;

                XnaToFnaHelper.Log($"[ProxyForm] Last window size: {WindowedBounds.Width} x {WindowedBounds.Height}");

                DisplayMode dm = gdm.GraphicsDevice.DisplayMode;
                // This feels so wrong.
                gdm.PreferredBackBufferWidth = dm.Width;
                gdm.PreferredBackBufferHeight = dm.Height;
                gdm.IsFullScreen = true;
                gdm.ApplyChanges();

                _Bounds = SDLBounds;

            } else {
                if (wasFakeFullscreenWindow) {
                    XnaToFnaHelper.Log("[ProxyForm] Leaving fake borderless fullscreen.");
                    gdm.IsFullScreen = false;
                }

                game.Window.IsBorderlessEXT = borderless;

                if (maximized) {
                    SDL.SDL_MaximizeWindow(window);
                    _Bounds = SDLBounds;
                } else {
                    SDL.SDL_RestoreWindow(window);
                    SDLBounds = _Bounds = WindowedBounds;
                }

                // This also feels so wrong.
                XnaToFnaHelper.Log($"[ProxyForm] New window size: {_Bounds.Width} x {_Bounds.Height}");
                gdm.PreferredBackBufferWidth = _Bounds.Width;
                gdm.PreferredBackBufferHeight = _Bounds.Height;
                gdm.ApplyChanges();
            }



            Dirty = false;
        }

    }

}
