<center>
    <p align="center">
        <a href="https://github.com/thnhmai06/SlideGenerator/releases"><img src="https://img.shields.io/github/release/thnhmai06/SlideGenerator?style=flat-square" alt="Latest Release" /></a>
        <a href="https://github.com/thnhmai06/SlideGenerator/releases"><img src="https://img.shields.io/github/downloads/thnhmai06/SlideGenerator/total?style=flat-square&color=blue" alt="Downloads" /></a>
        <a href="https://www.codefactor.io/repository/github/thnhmai06/SlideGenerator"><img src="https://www.codefactor.io/repository/github/thnhmai06/SlideGenerator/badge?style=flat-square" alt="CodeFactor" /></a><a href="https://ghloc.vercel.app/thnhmai06/SlideGenerator"><img src="https://img.shields.io/endpoint?url=https://ghloc.vercel.app/api/thnhmai06/SlideGenerator/badge%3Ffilter=.ts$,.tsx$,.html$,.css$,.cs$%26format=human&style=flat-square&color=blue" alt="Lines of Code" /></a><a href="https://github.com/thnhmai06/SlideGenerator/blob/main/LICENSE"><img src="https://img.shields.io/github/license/thnhmai06/SlideGenerator?style=flat-square" alt="License" /></a>
    </p>
    <h1 align="center">Slide Generator</h1>  
    <h4 align="center">
        An offline desktop tool to auto-generate PowerPoint slides from templates and spreadsheet data.
    </h4>
    <h5 align="center">Cross-platform, parallel-processing support, no Office required.</h5>
</center>

## Features

- **Automated Slide Generation:** Instantly create PowerPoint presentations from Excel spreadsheets and PPTX templates.
- **Intelligent Image Processing:**
  - **Smart ROI (Region of Interest) Strategies:**
    - **Rule of Thirds (Face Focus):** Detect faces and align them with the "rule of thirds" grid for professional photographic composition.
    - **Prominent (Saliency):** Automatically identifies and preserves the most visually striking or important region of the image.
    - **Center:** Traditional center-point anchoring for standard layouts.
  - **Precision Cropping Modes:**
    - **Fit:** Automatically calculates the optimal aspect ratio and scales the image to fit perfectly within the target shape without distortion.
    - **Crop:** Performs a direct cut based on the target dimensions for pixel-perfect results.
- **Cloud-Ready Data Handling:**
  - **Auto-Resolve Cloud Links:** Supports direct image resolution from Google Drive, OneDrive, and Google Photos.
  - **Automated Downloading:** Automatically fetches remote images during the generation process, eliminating manual downloads.
- **Offline & Private:** Runs 100% locally on your desktop. No internet connection required for core generation (Cloud features require temporary access).
- **No Office Needed:** Generates slides without requiring Microsoft Office or PowerPoint to be installed.
- **Robust Job Management:**
  - **Real-time Monitoring:** Track progress and status of every job and sheet.
  - **Control:** Pause, resume, cancel, or remove jobs at any time.
  - **Resilience:** Automatically saves job state; keeps your progress safe even if the app closes unexpectedly.
- **Modern UI/UX:**
  - Clean, responsive interface with Dark/Light theme support.
  - Multi-language support (English, Vietnamese).
- **Performance:**
  - Parallel processing for faster generation.
  - Cross-platform support (Windows, Linux).

## Installation

### Prerequisites

To run Slide Generator, you need to install the following runtime:

- [ASP.NET Core 10 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/10.0/runtime) (Choose the **Run server apps** option).

### Setup

1. **Download:** Get the latest release compatible with your platform from the [Releases page](https://github.com/thnhmai06/SlideGenerator/releases/latest).
2. **Run:** Launch the application by running the executable file (Setup/Protable).

## License

This project is licensed under the GPL-3.0 License - see the [LICENSE](LICENSE) file for details.

## Star History

<a href="https://www.star-history.com/#thnhmai06/SlideGenerator&type=timeline&legend=top-left">
 <picture>
   <source media="(prefers-color-scheme: dark)" srcset="https://api.star-history.com/svg?repos=thnhmai06/SlideGenerator&type=timeline&theme=dark&legend=top-left" />
   <source media="(prefers-color-scheme: light)" srcset="https://api.star-history.com/svg?repos=thnhmai06/SlideGenerator&type=timeline&legend=top-left" />
   <img alt="Star History Chart" src="https://api.star-history.com/svg?repos=thnhmai06/SlideGenerator&type=timeline&legend=top-left" />
 </picture>
</a>

## Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details on how to set up the development environment, build the project, and submit changes.

## Contributors

| [<img src="https://github.com/thnhmai06.png" width="100"><br><sub>**thnhmai06**</sub>](https://github.com/thnhmai06) | [<img src="https://github.com/NAV-adsf23fd.png" width="100"><br><sub>**NAV-adsf23fd**</sub>](https://github.com/NAV-adsf23fd) |
| :------------------------------------------------------------------------------------------------------------------: | :---------------------------------------------------------------------------------------------------------------------------: |
|                      <span title="Project Manager">👑</span> <span title="Developer">💻</span>                       |                                             <span title="UI/UX Concept">🎨</span>                                             |

**Core Framework:** [SlideGenerator.Framework](https://github.com/thnhmai06/SlideGenerator.Framework)
