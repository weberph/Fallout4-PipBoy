# Fallout4-PipBoy
Library to use the TCP communication mechanism/protocol from PipBoy Android/iOS app with .NET

### [WIP] Sending commands

**Toggle radio station:**
```csharp
    // ...
    var commandSender = new CommandSender(stream);      // get stream from network or file
    commandSender.Send(new Command(CommandType.ToggleRadio, id));
    // ...
```

**Toggle quest:**
```csharp
    // ...
    var commandSender = new CommandSender(stream);      // get stream from network or file
    commandSender.Send(new Command(                     // get quest object from GameStateReader
                        CommandType.ToggleQuest, (uint)quest.formID, (uint)quest.instance, (uint)quest.type));
    // ...
```

### Library usage

The game state can be tracked with the `GameStateReader` class and accessed via a dynamic object:
```csharp
static void Main(string[] args)
{
    using (var streamProvider = new PipBoyStreamProvider())
    using (var stream = streamProvider.Connect("192.168.0.5", 27000))
    {
        var gameStateReader = new GameStateReader(stream);
        while (gameStateReader.NextState())
        {
            Console.WriteLine("Player X position: " + (float)gameStateReader.GameState.Map.World.Player.X);
        }
    }
}
```

Instead of connecting to the game, the data can also be read from file for easy debugging:
```csharp
    // ...
    using (var stream = streamProvider.ReadFile("data.dump"))
    // ...
```

See [PipBoyTest/Program.cs](PipBoyTest/Program.cs) for an example.

##### Events

It is possible to register for the `Changed` event of a game object:
```csharp
var gameStateReader = new GameStateReader(stream);      // get stream from network or file
gameStateReader.NextState();                            // read first state

var playerPosition = (GameObject) gameStateReader.GameState.Map.World.Player;
playerPosition.Changed += PlayerPosition_Changed;       // register for Changed event

while (gameStateReader.NextState())                     // event are raised before NextState returns
{
    // do nothing
}

// ...

private static void PlayerPosition_Changed(object sender, GameObjectChangedEvent e)
{
    foreach (var changedChild in e.ChangedChildren)
    {
        Console.WriteLine("position changed: " + changedChild.ToString(true));
    }
}
```
Prints:
```
...
position changed: [32827: Map::World::Player::Rotation] = 89,26334
position changed: [32826: Map::World::Player::Y] = -47776,65
...
```


### Command line tool to dump data

To record data that can be used with the `StreamProvider::ReadFile` method, execute via command line:
```
> PipBoyDump.exe -c 192.168.0.5 -r raw.dump
```
Objects can be dumped on the fly:
```
> PipBoyDump.exe -c 192.168.0.5 -r raw.dump -g objects.txt
```
Objects can also be created from file:
```
> PipBoyDump.exe -f raw.dump -g objects.txt
```

##### Options
```
 -c, --connect        Host to connect to.
 -p, --port           (Default: 27000) Port to connect to.
 -f, --file           Input file (instead of ip/port).
 -r, --raw            File to write raw data received via network.
 -g, --gameobjects    File to write the structured game objects.
```
