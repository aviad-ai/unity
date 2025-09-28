<p align="center">
    <a href="https://aviad.ai" target="_blank">
        <img width="200" src="Images/logo.png" alt="Aviad Logo">
    </a>
</p>
<p align="center">
    <h1 align="center">
        Aviad AI (SLMs/LLMs) For Unity
    </h1>
    <h3 align="center">
        AI models for dynamic game systems in Unity.
    </h3>
</p>
<p align="center">
    <a href="https://github.com/aviad-ai/aviad"><img src="https://img.shields.io/badge/version-1.0.0-ff00a0.svg?style=flat-square"></a>
    &nbsp;
    <a href="https://github.com/aviad-ai/aviad/blob/main/LICENSE"><img src="https://img.shields.io/badge/license-MIT-00bfff.svg?style=flat-square"></a>
</p>
<p align="center">
    <a href="https://x.com/aviadai"><img src="https://img.shields.io/badge/twitter-Follow_us-1d9bf0.svg?style=flat-square"></a>
    &nbsp;
    <a href="https://www.linkedin.com/company/aviad-ai/"><img src="https://img.shields.io/badge/linkedin-Connect_with_us-0a66c2.svg?style=flat-square"></a>
</p>

## Overview
At Aviad, we're focused on bringing on-device AI models to games. These models are small enough to run locally on CPU and are more controllable through finetuning. They have the added advantage of supporting low-end systems and not competing with a game's graphics needs.

This package is an easy way to integrate LLMs into the Unity engine. It runs on top of a build of [llama.cpp](https://github.com/ggml-org/llama.cpp). The native code has only been built to run on CPU. Please reach out if you'd like GPU support.

## Platform Support

This package should support Windows, MacOS, and WebGL. Please create an issue if you run into trouble! Or join our [Discord](https://discord.gg/Jk4jUYghnA).

## Setup

1. Add via Unity Package Manager. `Window -> Package Manager`.

2. In the top left, click `+` and then `Install package from Git URL...`

3. Enter https://github.com/aviad-ai/unity.git in the text field that appears. Click `Install`.

4. `Packages/manifest.json` should contain a line like:

`    "ai.aviad.core": "https://github.com/aviad-ai/unity.git",`

You may pin to a version or commit like below:

`    "ai.aviad.core": "https://github.com/aviad-ai/unity.git#0.3.0",`

## TTS Usage

#### 1. Create the `Aviad.TTSRunner` component to your game. 
* Create an empty GameObject.
* Add component and select `TTSRunner`

#### 2. Download a TTS finetune from the Aviad platform.
* The finetune file is a .zip contain models in the .gguf format and pre-encoded reference audio to improve quality.
* Move this directory into your Assets/ directory.
* Select this directory within the `TTSRunner` inspector view.

#### 3. See `Samples/TTSDemo` for an example of usage!
* Call `runner.LoadTTS(Action<bool> onDone = null)`
* Call `runner.GenerateTTS(string text, Action<float[]> onAudio, Action<bool> onDone = null)`
* Use Unity's `AudioClip` or another audio library to interpret the float[] as wav data.

## LLM Usage

#### 1. Attach the `Aviad.Runner` component to your game. `Aviad.Runner` is an interface for configuring how the model is run.
* Create an empty GameObject.
* Add component and select `Aviad.Runner`

#### 2. Configure `Aviad.Runner` in the inspector view.
* Create a new ModelConfiguration asset to hold the model settings. Or use an existing one from the package.
* Set `Model Asset` field.
* Open the ModelConfiguration asset and click *Download Model*

<img width="300" src="Images/AviadRunner.png" alt="Aviad.Runner inspector view">

#### 3. Implement a script to interface with `Aviad.Runner`. At a high level, these methods will set up and run the model:
* Call `AddTurnToContext` to add to the model's context at runtime. Example usage:

  `aviadRunner.AddTurnToContext("system", "You are a helpful assistant.");`
  
  `aviadRunner.AddTurnToContext("user", "What's 2+2?");`

* Call `Generate` to receive streaming output from the model given the current context. After generation, the active context will include the model's output. `Generate` takes onUpdate and onDone functions that you can implement to specify things like how the generated text will get displayed, what other game mechanics are triggered, etc.
* Call `Reset` to clear the current conversation context completely.

#### 4. See [UnitySamples](https://github.com/aviad-ai/UnitySamples) for example usage and inspiration!

## Sample Games

We have one sample game so far that you can take and use:
* [The Tell-Tale Heart](https://github.com/aviad-ai/TheTellTaleHeart) - our first game demo that showcases
an SLM making and explaining in-character choices. It's free to play on [itch](https://aviadai.itch.io/the-tell-tale-heart).

<a href="https://www.youtube.com/watch?v=z-lg043BYF8">
  <img src="https://img.youtube.com/vi/z-lg043BYF8/0.jpg" alt="Watch the demo" width="480">
</a>

## Finetuned SLMs

We are building a library of open-source SLMs finetuned for specific game tasks. Check them out [on Hugging Face](https://huggingface.co/aviad-ai).
If you're interested in a custom SLM for your game, please reach out! We can help you finetune one.

## Reach Out
Star ⭐ this project and join us on [Discord](https://discord.gg/Jk4jUYghnA)! We post about the latest model, demo, and feature releases there.