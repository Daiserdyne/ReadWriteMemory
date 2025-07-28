# ReadWriteMemory

**ReadWriteMemory** is a .NET library that provides powerful, low‑level read/write access to the memory of external processes. It’s designed primarily for single‑player trainers, debugging tools, modding utilities, or any scenario where you need to:

- Read or write unmanaged data types (`int`, `float`, any other unmanaged struct)
- Read/write raw bytes
- Freeze or continuously monitor values
- Inject custom assembly (“code caves”)

> ⚠️ **Legal & ethical notice:**  
> Use this library **only** for legitimate, offline/single‑player scenarios (debugging, modding, personal tooling). Misuse in online/multiplayer environments may violate ToS or laws.

---

## 📦 Installation

Clone or add as a submodule:

```bash
git clone https://github.com/Daiserdyne/ReadWriteMemory.git
```

```csharp
using ReadWriteMemory.External;
using ReadWriteMemory.External.Entities;

var rw = new RwMemory("MyGameProcess");

// Write an integer (e.g. set health to 999)
var success = rw.WriteValue(new MemoryAddress(0x00ABCDEF), 999);

// Read a float
if (rw.ReadValue(new MemoryAddress("game.exe", 0x00500000, 0x10, 0x20), out float speed))
    Console.WriteLine($"Current speed: {speed}");

// Continuously monitor a value every 200ms
rw.ReadValueConstant<float>(
  new MemoryAddress(0x00ABCDEF),
  current => Console.WriteLine($"HP: {current}"),
  TimeSpan.FromMilliseconds(200)
);

// Freeze ammo count at 50 every 100ms
rw.FreezeValue<int>(
  new MemoryAddress(0x00FEDCBA),
  50,
  TimeSpan.FromMilliseconds(100)
);

// Create or resume a code cave
var cave = rw.CreateOrResumeCodeCave(
  new MemoryAddress(0x00400000),
  new byte[] { /* your custom machine code */ },
  amountOfOpcodesToReplace: 5,
  totalAmountOfOpcodesToReplace: 14
);
```

## 🧩 Core API
Initialization & Lifecycle

```csharp
new RwMemory(string processName)
```

Starts a background monitor that tracks process startup/exit and manages handles/modules.

```csharp
Dispose()
```
Restores all modified bytes, closes code caves, unfreezes values, stops loops, and closes the process handle.

#### Basic Read/Write
```csharp
bool WriteValue<T>(MemoryAddress addr, T value) where T : unmanaged
```
Writes any unmanaged type to addr.

bool WriteBytes(MemoryAddress addr, ReadOnlySpan<byte> data)
Writes raw bytes to addr.

```csharp
bool ReadValue<T>(MemoryAddress addr, out T value) where T : unmanaged
```

Reads an unmanaged type from addr.

bool ReadBytes(MemoryAddress addr, uint length, out byte[] buffer)
Reads length raw bytes from addr.

Continuous / Frozen Operations
```csharp
bool ReadValueConstant<T>(MemoryAddress addr, ReadValueCallback<T> callback, TimeSpan interval)
```

Periodically reads a T and invokes callback.

bool ReadBytesConstant(MemoryAddress addr, uint length, ReadBytesCallback callback, TimeSpan interval)
Periodically reads raw bytes and invokes callback.

```csharp
bool StopReadingValueConstant(MemoryAddress addr)
```

Stops a previously started constant read.

```csharp
bool FreezeValue<T>(MemoryAddress addr, T value, TimeSpan interval)
```

Overwrites addr with value every interval (“hard freeze”).

```csharp
bool FreezeValue<T>(MemoryAddress addr, TimeSpan interval) where T : unmanaged
```

Reads current value once and then freezes it.

```csharp
bool FreezeBytes(MemoryAddress addr, TimeSpan interval, uint bufferSize)
```

Reads raw bytes once and then freezes them.

bool UnfreezeValue(MemoryAddress addr)
Stops a previously started freeze.

#### Replace & Undo Bytes
```csharp
bool ReplaceBytes(MemoryAddress addr, ReadOnlySpan<byte> replacement)
```

Saves original opcodes internally, writes replacement.

bool UndoReplaceBytes(MemoryAddress addr)
Restores the original opcodes at addr.

#### Code Caves & ASM Injection
```csharp
CodeCaveTable CreateOrResumeCodeCave(MemoryAddress addr, ReadOnlySpan<byte> caveCode, int replaceCount,
int totalReplaceCount, uint allocSize = 4096)
```

Allocates memory in target, writes your caveCode, patches original instructions to jump into it. If already exists, re‑uses it.

#### Restores original bytes without freeing allocated cave memory.

```csharp
bool PauseOpenedCodeCave(MemoryAddress addr)
```

#### Restores original bytes and deallocates the cave region.
```csharp
bool CloseCodeCave(MemoryAddress addr)
```

## 🔧 Internals & Events
event ProcessStateHasChanged OnProcessStateChanged
Fires when the target process starts or exits.

event ReInitializeTargetProcess OnReInitializeTargetProcess
Fires when internal handles/modules are reset.

## 🛠️ Planned / Community Features
- AOB & pattern scanning
- Pointer‑chain helpers
- Cross‑platform support (Linux /proc)
- Visual memory inspector UI
- Official NuGet package + CI badges

# Contributions and ideas are very welcome—feel free to open an Issue or PR!