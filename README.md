# 🎬 IMDB-recorder

A Windows Forms application for keeping a personal record of movies and TV series you've watched, with search functionality.

## Features

- Add movies or series you've watched
- Search through your collection
- Simple Windows Forms interface
- Organized code with separate classes
- Uses local .mdf database for data storage

## Technologies Used

- C#
- .NET Framework 4.5
- SQL Server LocalDB (for .mdf database)
- System.Data.SqlClient
- Windows Forms

## Prerequisites

The installer (Setup.exe) automatically installs:
- .NET Framework 4.5
- SQL Server 2012 Express LocalDB

No manual installation needed.

## How to Install

1. Download `Setup.exe` and `My IMDB.msi` from this repository
2. Run `Setup.exe` (not the .msi file alone)
3. Follow the installation wizard
4. Launch from Start Menu or Desktop shortcut

## How to Run from Source

1. Clone this repository
2. Open `My IMDB.sln` in Visual Studio
3. Press F5 to run the application

## Project Structure
- `My IMDB/` - Main application files
- `My IMDB.sln` - Visual Studio solution file
- `.gitignore` - Git ignore rules
- `.gitattributes` - Git attributes

## Troubleshooting

| Problem | Solution |
|---------|----------|
| Database connection error | SQL Server LocalDB not installed. Run Setup.exe again. |
| Setup.exe does nothing | Temporarily disable antivirus and retry |

## Author

Mohammad Mirzaee

GitHub: [@mohammadmirzaee25](https://github.com/mohammadmirzaee25)
