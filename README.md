LDPrint
--------------

[![MIT licensed](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.md)

LDPrint is intended as a tool to convert an LDraw parts library into a file format more suitable for 3D-printing.

Through this project I'm mostly looking to get some experience with FParsec and the LDraw / 3MF file formats. I plan to use it to print some large-scale (5-6x) models for use as gifts.

Roadmap
--------------
- [ ] Input Formats
	- [x] LDraw File Format Parser
	- [ ] LDraw Part Library Porcessing
- [ ] Output Formats
    - [ ] 3MF
    - [ ] STL?
- [ ] 3D-Printability	
	- [ ] Scale
	- [ ] Fit Tolerance

Building
--------------
``` 
dotnet build
```

Testing
--------------
``` 
dotnet test
```

Usage
--------------

- Obtain an LDraw parts library from [ldraw.org](https://www.ldraw.org/parts/latest-parts.html) and unzip to a location of your choice, then:

```
USAGE: LDPrint.CLI.exe [--help] [--dry-run] [--input-path <partPath>] [--output-path <outputPath>] [--scale <decimal>]

OPTIONS:

    --dry-run             Executes without writing files.
    --input-path, -i <partPath>
                          LDraw parts library path.
    --output-path, -o <outputPath>
                          Output path. (default = './output')
    --scale, -s <decimal> Output scale. (default = 1, roughly 1 LDU = 0.4mm.)
    --help                display this list of options.
```
