# NextUI Plugin

[![](https://i.imgur.com/C14liOC.png)](https://discord.gg/2mMbXD4HQN)

Key differences between `NU` and `BrowserHost`:

- More options
- Built in websocket server which you can enquire for additional data
- No IPC (CEF initialized within same process)
- Preinstalled [NextUI](https://kaminaris.github.io/Next-UI/?OVERLAY_WS=ws://127.0.0.1:10501/ws)
overlay (which can be removed)
- Support for visibility flags such as hiding outside of combat
- Ability to precisely set size and position

## Installation:

Requires to have dalamud installed, for windows users there is nothing
else that needs to be done aside from:

`dotnet restore`

For other OSes, you have to have dalamud downloaded from
[here](https://goatcorp.github.io/dalamud-distrib/stg/latest.zip)
and extracted into `dalamud` folder in root project directory.
Consult `.gitlab-ci.yml` for more details.

### Bleeding edge repository

If you are not interested (or do not possess development skills) to build
plugin by yourself, custom repository has been made:

Thanks to gitlab pipeline this plugin has bleeding edge repository that
gets all the nightly updates:
[https://gitlab.com/kaminariss/nextui-repo/-/raw/main/pluginmaster.json](https://gitlab.com/kaminariss/nextui-repo/-/raw/main/pluginmaster.json)

## Build

### Debug

**WARNING**: Do NOT use `Debug` for everyday usage.

due to how [CEFSharp](https://cefsharp.github.io/) works, it is actually
impossible to reload plugin. CEF cannot be reinitialized within same
process and to make it work, some really nasty things are made.

In short, it kills Cef subprocesses manually while making copy of entire
CEF directory (100Mb) each time it's loaded. **This mode is only meant for
pure development.**

### Release

Release has special mechanism to download another part of a plugin called
internally `Mirocplugin` which corresponds to `NextUIBrowser` project.

Mechanism is pretty simple, main plugin is able to update itself as long as
it does not initialize `CEF`. And to check that, we save game process ID
to a text file.

This state is called *Cold boot*

While in *Cold boot*, if required version is higher than installed
(or there is no microplugin downloaded yet) main plugin first downloads
microplugin and then loads it. Then microplugin loads `CEF`.

Technically updating microplugin will be very rare.

## Technical details

In short, CEF renders webpage and whenever `update` occurs inside internal
browser engine, we save it's buffer to our own pointer (resizing it, if
size has changed). After that, we update the texture and draw that on DX
texture. We are passing key and mouse event in very similar fashion to how
BrowserHost used to do it with minor changes. Only difference so far is that
events can be passthru (originally `BrowserHost` kept focus). I do intend to
eventually fix mouse drag problems.

## Known issues

Rapidly resizing overlay may cause game to crash, so far I do not know
the solution to this.