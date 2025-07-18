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
At Aviad, we're focused on bringing small language models (SLMs) to games. SLMs are capable game assets that bring all the benefits of AI while being small 
enough to run locally on CPU and more controllable through finetuning. They have the added advantage of supporting low-end systems and not competing with a game's graphics needs.

This package is an easy way to integrate LLMs into the Unity engine. It runs on top of a build of [llama.cpp](https://github.com/ggml-org/llama.cpp). The native code has only been built to run on CPU. Please reach out if you'd like GPU support.

## Platform Support

This package should support Windows, MacOS, and WebGL. Please create an issue if you run into trouble! Or join our [Discord](https://discord.gg/Jk4jUYghnA).

## Quick Start Video Tutorial
Minute long YouTube tutorial to get started 👇

<a href="https://www.youtube.com/watch?v=ISI8tTZ8gwc">
  <img src="https://img.youtube.com/vi/ISI8tTZ8gwc/0.jpg" alt="Watch the demo" width="480">
</a>

## Setup

1. Add via Unity Package Manager. `Window -> Package Manager`.

2. In the top left, click `+` and then `Install package from Git URL...`

3. Enter https://github.com/aviad-ai/unity.git in the text field that appears. Click `Install`.

4. `Packages/manifest.json` should contain a line like:

`    "ai.aviad.core": "https://github.com/aviad-ai/unity.git",`

You may pin to a version or commit like below:

`    "ai.aviad.core": "https://github.com/aviad-ai/unity.git#0.1.0",`

## Usage

#### 1. Attach the `AviadRunner` component to your game. `AviadRunner` is an interface for configuring how the model is run.
* Create an empty GameObject.
* Add component and select `AviadRunner`

#### 2. Configure `AviadRunner` in the inspector view.

<img width="300" src="images/AviadRunnerConfig.png" alt="AviadRunner inspector view">

*Model Configuration*

* `Model Url` - provide any url to download a `.gguf` model. For example, link to a huggingface model like:

`https://huggingface.co/bartowski/Llama-3.2-1B-Instruct-GGUF/resolve/main/Llama-3.2-1B-Instruct-Q4_K_M.gguf?download=true`

* `Save To Streaming Assets` - if unchecked then download will begin upon game start. Otherwise, click `DownloadModel` to save the model file in the StreamingAssets folder.
* `Continue Conversation After Generation` - If checked, the model will maintain conversational context across multiple turns (i.e., messages) instead of starting fresh each time.
* `Max Context Length` - The maximum number of tokens the model can use as input context. Should match the model's trained context length (e.g., 4096).
* `GPU Layers` - Number of layers to offload to the GPU. Please set to 0 meaning the model runs entirely on CPU.
* `Threads` - Number of threads to use for inference. Higher values can improve performance on multicore CPUs.
* `Max Batch Length` - The maximum number of tokens that can be processed in a single batch during inference. Used to tune performance.

*Generation Configuration*

* `Chat Template` - Name of the prompt template used for chat formatting (e.g., "chatml", "llama2", etc.). Required for proper context structuring.
* `Grammar String` - Optional grammar constraints for output generation. Use structured syntax if supported by the model (e.g., GBNF).
* `Temperature` - Sampling temperature for randomness. Lower values (e.g., 0.2) make output more deterministic; higher values (e.g., 1.0) increase creativity.
* `Top P` - Controls nucleus sampling. The model samples from the smallest possible set of tokens whose cumulative probability exceeds this value. Lower = more focused.
* `Max Tokens` - Maximum number of tokens the model is allowed to generate in the response.
* `Chunk Size` - The number of tokens returned at a time when streaming output. Smaller values give faster feedback but may introduce latency.

#### 3. Implement a script to interface with `AviadRunner`. At a high level, these methods will set up and run the model:
* Call `AddTurnToContext` to add to the model's context at runtime. Example usage:

  `aviadRunner.AddTurnToContext("system", "You are a helpful assistant.");`
  
  `aviadRunner.AddTurnToContext("user", "What's 2+2?");`

* Call `Generate` to receive streaming output from the model given the current context. After generation, the active context will include the model's output. `Generate` takes onUpdate and onDone functions that you can implement to specify things like how the generated text will get displayed, what other game mechanics are triggered, etc.
* Call `Reset` to clear the current conversation context completely.

#### 4. See [UnitySamples](https://github.com/aviad-ai/UnitySamples) for example usage and inspiration!
See more details in the section below.

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
If you're interested in a custom SLM for your game, please reach out! We can help you finetune one.

## Reach Out
Star ⭐ this project and join us on [Discord](https://discord.gg/Jk4jUYghnA)! We post about the latest model, demo, and feature releases there.
