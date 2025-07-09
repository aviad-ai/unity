# Aviad Unity Package

Our approach is focused on building with the smallest language models available. This has the advantage of supporting low-end systems and not competing with a game's graphics needs.

### Support

This package should support Windows, MacOS, and WebGL. Please create an issue if you run into trouble! Or join our discord: https://discord.gg/Jk4jUYghnA

The package runs on top of a build of [llama.cpp](https://github.com/ggml-org/llama.cpp). The native code has only been built to run on CPU.

### Setup

1. Add via Unity Package Manager. `Window -> Package Manager`.

2. In the top left, click `+` and then `Install package from Git URL...`

3. Enter https://github.com/aviad-ai/unity.git in the text field that appears. Click `Install`.

4. `Packages/manifest.json` should contain a line like:

`    "ai.aviad.core": "https://github.com/aviad-ai/unity.git",`

You may pin to a version or commit like below:

`    "ai.aviad.core": "https://github.com/aviad-ai/unity.git#0.1.0",`

### Usage

1. Attach the `AviadRunner` component to your game.
* Create an empty GameObject.
* Add component and select `AviadRunner`

2. Configure `AviadRunner` in the inspector view.

* In the `Model Url` field, provide any url to download a `.gguf` model. For example, link to a huggingface model like:

`https://huggingface.co/bartowski/Llama-3.2-1B-Instruct-GGUF/resolve/main/Llama-3.2-1B-Instruct-Q4_K_M.gguf?download=true`

* If `Save To Streaming Assets` is unchecked then download will begin upon game start. Otherwise, click `DownloadModel` to save the model file in the StreamingAssets folder.
* See [llama.cpp](https://github.com/ggml-org/llama.cpp) for an overview of the remaining settings.

3. Implement a script to interface with `AviadRunner`.

* Call `AddTurnToContext` to add to the model's context at runtime.
* Call `Generate` to receive streaming output from the model given the current context. After generation, the active context will include the model's output.
* Call `Reset` to reset the context completely.

4. See [UnitySamples](https://github.com/aviad-ai/UnitySamples) for example usage and inspiration!

