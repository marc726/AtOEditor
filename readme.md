## Across the Obelisk Decryptor/Encryptor

Tested on Across the Obelisk v1.5.0.1

The project targets .NET 8.0 (net8.0-windows)

### Features

- **Decrypt and Edit Saves:**  
  Drag and drop an encrypted `.ato` save file to the binary (.exe file), decrypts it to json. (decryption/encryption untested on .ato files that are not player.ato or gamedata_#.ato)
  - Note: Original save files can be found in `\AppData\LocalLow\Dreamsite Games\AcrossTheObelisk\<UserID>` for Windows.
  
- **Re-Encryption:**  
  After editing, the save file can be re-serialized and re-encrypted, producing a new `.ato` file by dragging and dropping again to the binary (.exe file).

#### Contributions:
 - Contributions are always welcome. In order to successfully build this binary, two .dlls are required to be put into `./lib/` which are `Assembly-CSharp.dll` and `UnityEngine.CoreModule.dll`. You can find these in your game install folder.

#### Disclaimer:
Use this tool at your own risk. Always back up your save files before using the editor. I am not responsible for any data loss or corruption.

