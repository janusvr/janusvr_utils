JanusVR Unity Exporter
Latest Release: v2.11

FAQ




Dev Info
- The Unity package include source files for the exporter instead of a built DLL for compatibility with Unity's from version 5.0 to 5.6+ (with static DLLs we'd have to update 7 packages every time we release a new update to the exporter).
- If there's code that looks redundant be careful of changing it - Unity can be extremely finnicky, and a lot of ifs are just there to handle extremely rare situations that could lead to a crash.
- All the methods that display a progress bar use a try/catch/finally because if it fails anywhere Unity will not hide the progress bar - locking the user out of the editor.

- The conversion between Unity and Janus space is made on the RoomObject class - SetUnityObj method.