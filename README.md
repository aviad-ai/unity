<p align="center">
    <a href="https://aviad.ai" target="_blank">
        <img width="200" src="images/logo.png" alt="Aviad Logo">
    </a>
</p>
<p align="center">
    <h1 align="center">
        Aviad AI (SLMs/LLMs) For Unity
    </h1>
    <h3 align="center">
        Language models for dynamic game systems in Unity.
    </h3>
</p>
<p align="center">
    <a href="https://github.com/aviad-ai/unity"><img src="https://img.shields.io/badge/version-1.0.0-ff00a0.svg?style=flat-square"></a>
    &nbsp;
    <a href="https://github.com/aviad-ai/unity/blob/main/LICENSE"><img src="https://img.shields.io/badge/license-MIT-00bfff.svg?style=flat-square"></a>
</p>
<p align="center">
    <a href="https://x.com/aviadai"><img src="https://img.shields.io/badge/twitter-Follow_us-1d9bf0.svg?style=flat-square"></a>
    &nbsp;
    <a href="https://www.linkedin.com/company/aviad-ai/"><img src="https://img.shields.io/badge/linkedin-Connect_with_us-0a66c2.svg?style=flat-square"></a>
</p>

## Overview
At Aviad, we're focused on bringing small language models (SLMs) to games. SLMs are capable game assets that bring all the benefits of AI while being small 
enough to run locally on CPU and more controllable through finetuning. They have the added advantage of supporting low-end systems and not competing with a game's graphics needs.

This package is an easy way to integrate LLMs into the Unity engine. It runs on top of a build of [llama.cpp](https://github.com/ggml-org/llama.cpp). The native code has only been built to run on CPU.

## Platform Support

This package should support Windows, MacOS, and WebGL. Please create an issue if you run into trouble! Or join our [Discord](https://discord.gg/Jk4jUYghnA).

## Quick Start Video Tutorial
Minute long YouTube tutorial to get started 👇

<a href="https://www.youtube.com/watch?v=ISI8tTZ8gwc">
  <img src="https://img.youtube.com/vi/ISI8tTZ8gwc/0.jpg" alt="Watch the demo" width="480">
</a>

### Setup

1. Add via Unity Package Manager. `Window -> Package Manager`.

2. In the top left, click `+` and then `Install package from Git URL...`

3. Enter https://github.com/aviad-ai/unity.git in the text field that appears. Click `Install`.

4. `Packages/manifest.json` should contain a line like:

`    "ai.aviad.core": "https://github.com/aviad-ai/unity.git",`

You may pin to a version or commit like below:

`    "ai.aviad.core": "https://github.com/aviad-ai/unity.git#0.1.0",`

### Usage

1. Attach the `AviadRunner` component to your game. `AviadRunner` is an interface for configuring how the model is run.
* Create an empty GameObject.
* Add component and select `AviadRunner`

2. Configure `AviadRunner` in the inspector view.

* In the `Model Url` field, provide any url to download a `.gguf` model. For example, link to a huggingface model like:

`https://huggingface.co/bartowski/Llama-3.2-1B-Instruct-GGUF/resolve/main/Llama-3.2-1B-Instruct-Q4_K_M.gguf?download=true`

* If `Save To Streaming Assets` is unchecked then download will begin upon game start. Otherwise, click `DownloadModel` to save the model file in the StreamingAssets folder.
* See [llama.cpp](https://github.com/ggml-org/llama.cpp) for an overview of the remaining settings.

3. Implement a script to interface with `AviadRunner`. At a high level, these methods will set up and run the model:
* `InitializeModel` and `Initialize Context` needs to be called once at the start of the game.
* Call `AddTurnToContext` to add to the model's context at runtime.
* Call `Generate` to receive streaming output from the model given the current context. After generation, the active context will include the model's output.
* Call `Reset` to reset the context completely.
There are some helper and callback functions that will help you debug and before precisely build mechanics around the model.

4. See [UnitySamples](https://github.com/aviad-ai/UnitySamples) for example usage and inspiration!

## Sample Games

We have two sample games so far that you can take and use:
* [BaseDialogueSample](https://github.com/aviad-ai/UnitySamples/tree/main/BaseDialogueSample) - very simple implementation
with chatbot like interface
* [The Tell-Tale Heart](https://github.com/aviad-ai/UnitySamples/tree/main/TheTellTaleHeart) - our first game demo that showcases
an SLM making and explaining in-character choices. It's free to play on [itch](https://aviadai.itch.io/the-tell-tale-heart).

<a href="https://www.youtube.com/watch?v=z-lg043BYF8">
  <img src="https://img.youtube.com/vi/z-lg043BYF8/0.jpg" alt="Watch the demo" width="480">
</a>

## Finetuned SLMs

We are building a library of open-source SLMs finetuned for specific game tasks. Check them out [on Hugging Face](https://huggingface.co/aviad-ai).
If you're interested in a specific SLM for your game, please reach out! We can help you finetune one.

## Reach Out
Star ⭐ this project and join us on [Discord](https://discord.gg/Jk4jUYghnA)! We post about the latest model, demo, and feature releases there.
