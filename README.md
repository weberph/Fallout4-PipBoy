# Fallout4-PipBoy
Use TCP communication mechanism from PipBoy Android/iOS app with .NET

### Update 20.11.2015
The game state can be tracked with the `GameStateReader` class and accessed via a dynamic object:
```csharp
// get stream from network or file
var gameStateReader = new GameStateReader(stream);
while (gameStateReader.NextState())
{
    Console.WriteLine("Player X position: " + (float)gameStateReader.GameState.Map.World.Player.X);
}
```

### Current state

- parsing and dumping of tcp communication works

- Partially inspects/dumps the contents of the initial packet including
the inventory, components, radio stations, perks, stats, special attributes, quests, player info, game status and more

- Other packets like movement, direction change or change of time are also dumped

#### Sample output:

excerpt from the initial received packet:
```
...

Caps: 4689
DateMonth: 11
PlayerName: *removed*
PerkPoints: 1
XPProgressPct: 0,5451092
CurrWeight: 266,225
CurrHP: 119,1287
XPLevel: 21
TotalDamages: [39759, 39762, 39765, 39768, 39771, 39774]
SlotResists: [39785, 39804, 39823, 39842, 39861, 39880, 39899, 39918, 39937]
MaxAP: 130
CurrentHPGain: 0
DateDay: 28
TotalResists: [39779, 39782]
MaxHP: 175
TimeHour: 23,15003
DateYear: 287
CurrAP: 130
MaxWeight: 410

IsInAnimation: False
IsPlayerInDialogue: False
EffectColor: [25, 26, 27]
IsPlayerPipboyLocked: False
IsLoading: False
IsPipboyNotEquipped: False
IsPlayerMovementLocked: False
IsInVats: False
IsInAutoVanity: False
IsInVatsPlayback: False
MinigameFormIds: [19981, 19982, 19983, 19984, 19985]
IsPlayerDead: False
IsMenuOpen: True
IsDataUnavailable: False

...
```

later received packets (here: walking/turning):
```
[15 - Status::IsMenuOpen] bool: False
[18 - Status::IsInAnimation] bool: True
[18 - Status::IsInAnimation] bool: False
[36483 - Map::World::Player::X] float: -79252,4
[36484 - Map::World::Player::Y] float: 90429,59
[36479 - Map::Local::Player::X] float: -79252,4
[36480 - Map::Local::Player::Y] float: 90429,59
[36479 - Map::Local::Player::X] float: -79261,21
[36479 - Map::Local::Player::X] float: -79271,63
[36485 - Map::World::Player::Rotation] float: 259,4895
[36479 - Map::Local::Player::X] float: -79280,4
[36481 - Map::Local::Player::Rotation] float: 259,4895
[36479 - Map::Local::Player::X] float: -79290,67
[36480 - Map::Local::Player::Y] float: 90421,22
[36485 - Map::World::Player::Rotation] float: 253,8014
[36479 - Map::Local::Player::X] float: -79302,8
[36481 - Map::Local::Player::Rotation] float: 253,8014
[36479 - Map::Local::Player::X] float: -79314,52
[36485 - Map::World::Player::Rotation] float: 248,1132
[36481 - Map::Local::Player::Rotation] float: 248,1132
[36480 - Map::Local::Player::Y] float: 90413,56
[36479 - Map::Local::Player::X] float: -79326,3
[36483 - Map::World::Player::X] float: -79331,76
[36484 - Map::World::Player::Y] float: 90405,96
...
[39756 - PlayerInfo::TimeHour] float: 23,16669
...
[36483 - Map::World::Player::X] float: -79685,98
[36480 - Map::Local::Player::Y] float: 90016,06
[36485 - Map::World::Player::Rotation] float: 198,866
[36481 - Map::Local::Player::Rotation] float: 198,866
[18 - Status::IsInAnimation] bool: True
[36514 - Map::Local::Extents::NWX] float: -82757,98
[36517 - Map::Local::Extents::NEY] float: 91744,06
[36516 - Map::Local::Extents::NEX] float: -76613,98
[36519 - Map::Local::Extents::SWY] float: 88288,06
[36518 - Map::Local::Extents::SWX] float: -82757,98
[36515 - Map::Local::Extents::NWY] float: 91744,06
[18 - Status::IsInAnimation] bool: False
[15 - Status::IsMenuOpen] bool: True
...
```

