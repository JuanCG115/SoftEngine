# SoftEngine 3D

A fully interactive, real-time 3D Software Rendering Engine built from scratch in C# using .NET and SharpDX. 
This engine performs all multi-vector math, coordinate transformations, deep scanline rasterization, Z-buffering, Gouraud illumination shading, and perspective-correct UV texture mapping manually on the CPU. 
It explicitly operates without hardware acceleration APIs (like OpenGL, DirectX, or WebGL), recreating the low-level processing pipeline of modern graphics cards entirely in software.

## Credits & Acknowledgments
The architectural rendering framework and core mathematical calculations of this project are strictly based on the exceptional multi-part educational tutorial series **"Learning how to write a 3D software engine from scratch"** authored by **David Rousset** in 2013. 

While preserving his excellent core algorithmic principles, this software has been significantly re-engineered, refactored, and expanded to support contemporary hardware pipelines and runtime asset interaction.

## Legacy Refactoring & Ecosystem Modernization
The original 2013 project architecture relied on tools and web wrappers that have since become obsolete. The following architectural adaptations were introduced to modernize the infrastructure:

| Original Feature (2013) | Modern Refactoring (2026) | Technical Necessity & Resolution |
| :--- | :--- | :--- |
| **`.babylon` JSON Structure** | **Native Wavefront `.obj` File Tokenizer** | Completely bypassed old unmaintained JSON formats and third-party JSON parsing libraries. Programmed a lightweight text stream tokenizer from scratch using pure C# string manipulation to parse industry-standard `.obj` files natively. |
| **`babylon.py` Export Script** | **Blender Mesh Triangulation Pipeline** | The original Python export addon is fundamentally broken on modern versions of Blender. Replaced by passing assets through Blender’s native **Triangulate Mesh** export modifier. |
| **Monolithic String Buffering** | **Streamed `File.ReadAllLines` Parsing** | Replaced massive undivided string memory allocations with streamed tokenization to prevent memory overhead and IDE compiler stalls. |
| **Sequential Single-Thread Render** | **Asynchronous `Parallel.For` Core Slicing** | Distributed heavy vertex and face coordinate arithmetic across all available CPU cores concurrently, wrapping the final raster buffer writes in atomic synchronization blocks (`lock`) to eliminate visual race conditions. |
| **WinJS / Windows 8 Web Frame** | **Desktop Windows Forms Application** | Migrated the codebase away from obsolete HTML5 app frameworks into a high-performance, double-buffered C# WinForms desktop engine utilizing `SharpDX` exclusively for native mathematical structures (`Matrix`, `Vector3`, `Color4`). |


## New Core Enhancements & Added Features

Beyond the scope of the original 3D engine tutorial, several custom features were implemented to provide full runtime control and interactive capabilities:

1. **Interactive Texture-Switching UI Menu (`ToolStrip`)**
   - Engineered a clean, non-obtrusive, native graphical overlay menu at the top of the viewport.
   - Built an event-driven `ChangeTexture(string filename)` router that hot-swaps active bitmapped textures at runtime without stalling the asynchronous drawing threads.
   - Designed automatic fallback handling to safely catch and flag file exceptions without crashing the rendering process.
2. **Keyboard Control Focus Retention**
   - Integrated an automatic window control re-focus scheme (`this.Focus()`) to bridge the WinForms focus conflict. When interacting with menu buttons, input polling instantly returns to the engine, preventing key-input blocking.
3. **Manual Viewport Manipulation Controls**
   - Programmed a direct input system to allow users to manually rotate, tilt, or scale object depth on demand instead of forcing an uncontrollable automatic spinning loop.

## Key Technical Mechanics Included

- **Gouraud Smooth Shading:** Simulates local surface lighting by calculating individual vertex normal direction arrays against a vector light source using Lambertian Dot Products ($cos \theta = N \cdot L$), then interpolating color intensities smoothly across polygons.
- **UV Texture Mapping:** Wraps flat 2D bitmap textures into 3D polygon arrays by mapping and interpolating normalized horizontal ($U$) and vertical ($V$) texture coordinates.
- **Z-Buffer Depth Resolution:** Solves spatial transparency, overlapping models, and surface occlusion by maintaining a dynamic screen-space float array tracking relative proximity to the camera plane.
- **Back-Face Culling:** Instantly drops hidden polygons before rasterization by evaluating face normal directions against the camera using Vector Cross Products ($A \times B$). This optimization effectively doubles the application framerate.

## Mesh Ingestion & Parsing Architecture (How `.obj` Assets are Loaded)

Instead of relying on third-party dependencies, the engine features a custom-built text tokenizer in `Device.cs` under the `LoadOBJFromFile` method. This pipeline maps unstructured string data into clean object structures in real-time through the following sequence:

1. **Streaming Input Initialization:** The engine reads the target Wavefront `.obj` file line-by-line via `File.ReadAllLines(fileName)`.
2. **Tokenization and Multi-Map Routing:** The parser strips whitespace and filters every row based on standard file format prefixes:
   - Lines beginning with `v ` are parsed into `SharpDX.Vector3` structural points representing vertex **Coordinates**.
   - Lines beginning with `vt ` are parsed into `SharpDX.Vector2` coordinates representing texture coordinate mapping (**UVs**). Crucially, since Blender flips the vertical image space, the engine manually inverts the channel during ingestion via `1.0f - V`.
   - Lines beginning with `vn ` are parsed into `SharpDX.Vector3` arrays capturing geometric surface vectors (**Normals**).
3. **Index Mapping and Polygon Construction:** When hitting face definitions (`f `), the parser splits data sequences separated by slashes (`/`), tracking which vertex coordinates hook into specific texture maps and illumination arrays. It subtracts `1` from all read indices to gracefully shift Blender’s 1-based indexing into C#'s 0-based array paradigm.
4. **Mesh Allocation:** The structured elements are packaged into a comprehensive `Mesh` instance, making it fully ready to be picked up by the parallelized matrix transformation pipeline during the `Render` sequence.

## Navigation & Keyboard Controls
The rendering viewport actively hooks into keyboard input to manipulate the environment in real time:

- **`W` Key** : Zoom camera view closer (Moves camera forward on the Z-axis).
- **`S` Key** : Zoom camera view further (Moves camera backward on the Z-axis).
- **`▲ Arrow Up`** : Tilt the 3D model upwards over its local X-axis.
- **`▼ Arrow Down`** : Tilt the 3D model downwards over its local X-axis.
- **`◄ Arrow Left`** : Spin the 3D model counter-clockwise over its local Y-axis.
- **`► Arrow Right`** : Spin the 3D model clockwise over its local Y-axis.

## Preview

- <img width="801" height="667" alt="image" src="https://github.com/user-attachments/assets/5dcbbe28-68d3-4b86-8e55-2448b1ff26c1" />
- <img width="800" height="667" alt="image" src="https://github.com/user-attachments/assets/c303428b-8b1e-4cb3-b02c-8f91aee5a07d" />

<p align="center">
  <video src="https://github.com/user-attachments/assets/59491e4f-3b9b-4baa-a98c-aff679c83429" width="60%" controls></video>
</p>


## How to Run and Test the Project

To build and run this engine locally on your machine, follow these direct deployment steps:

### Prerequisites
- **Visual Studio 2022** (Community, Professional, or Enterprise editions).
- **.NET Desktop Development Workload** (Enabled via the Visual Studio Installer).

### Step-by-Step Deployment

1. **Clone the Repository:**
   Open your preferred terminal or command line and fetch the project code locally:
   ```bash
   git clone [https://github.com/JuanCG115/SoftEngine.git](https://github.com/JuanCG115/SoftEngine.git)

2. **Open the Solution:**
   Navigate into the project root directory and double-click the standard `SoftEngine.sln` file to automatically load the entire workspace environment inside Visual Studio.

3. **Verify Asset Outputs (Crucial):**
   Inside the Solution Explorer, inspect your mesh assets (monkey.obj) and image channels (`texture1.png`, `texture2.png`, `texture3.png`).

   -Left-click each asset file to open its structural Properties window.

   -Verify that the "Copy to Output Directory" parameter is explicitly toggled to "Copy if newer" (or "Copy always"). This forces the compiler to pipeline the physical resources straight into the live executable target folder (`/bin/Debug/`).

4. **Compile & Execute:**
   Press `F5` or click the green "Start / Debug" button on the top toolbar.

   -The interactive engine window will immediately display the live texturized mesh overlay.

   -Click the interactive top menu to toggle active texture layouts, and use your arrow keys to manipulate the viewport environment freely!

## Exporting Custom Assets from Blender
To import your own custom 3D models into this engine, use the following configurations during Blender export:

1. Select your model and click **File > Export > Wavefront (.obj)**.
2. In the configurations side-panel under **Geometry**, make sure the following settings are **checked**:
   - `[x] UV Coordinates` *(Crucial for processing texture pixel maps)*
   - `[x] Normals` *(Crucial for Gouraud lighting physics)*
   - `[x] Triangulated Mesh` *(Forces geometry into 3-vertex polygons)*
   - `[x] Apply Modifiers`
   - `[x] Apply Transform`
3. Export your files and make sure your images (`texture1.png`, `texture2.png`, and `texture3.png`) are placed in your working directory with their **"Copy to Output Directory"** property set to **"Copy if newer"**.
