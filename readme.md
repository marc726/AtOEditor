## Across the Obelisk Deck Editor

This is a very early stage deck editor for the game Across the Obelisk. Currently, it only shows the info for all 4 characters and only you deck can be modified.


**.NET SDK:**  
The project targets .NET 8.0 (net8.0-windows)

### Features

- **Decrypt and Edit Saves:**  
  Opens an encrypted `.ato` save file, decrypts it, and displays character data.
  - These files can be found in `\AppData\LocalLow\Dreamsite Games\AcrossTheObelisk\<UserID>`for Windows.
  
- **Deck Editing:**  
  View character details and edit the deck for the active character. The UI includes:
  - A read-only section with hero details.
  - A deck list with search functionality.
  - An available card pool (populated from assets).
  - A preview area for card images.
  
- **Re-Encryption:**  
  After editing, the save file can be re-serialized and re-encrypted, producing a new `.ato` file.

#### Requirements:

 - Two .dlls are required to be put into `./lib/` which are `Assembly-CSharp.dll` and `UnityEngine.CoreModule.dll`. You can find these in your game install folder.

 - (Optional) Assets are to be pulled separately. The program relies on a specific method using the dev menu. You MUST label the folders accordingly: `/assets/cardimg/<subfolders here>`. These subfolders are labeled already if you extracted using the dev tool. While it is optional, I highly recommend this as naming scheme isn't implemented yet and just focusing on raw names parsed from JSON. Card images allow you to see effects. 

#### Disclaimer:
Use this tool at your own risk. Always back up your save files before using the editor. I am not responsible for any data loss or corruption.

