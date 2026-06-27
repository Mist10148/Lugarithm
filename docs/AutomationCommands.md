# Automation Mode - Command Reference

> Program the jeepney, press **RUN**, and watch the route play out. The sim uses
> grid logic for correctness, while procedural town movement animates smoothly
> along the generated road.

Automation has two editor surfaces:

- **Blocks** build the same program with draggable commands.
- **Code** writes the same logic in Python-style text.

Autopilot now fills the active editor with the reference solution first, then
runs it through the normal editor execution path. It does not secretly bypass
the player-facing program anymore.

## Actions

Actions do something and take a tick.

| Command | What it does |
|---|---|
| `moveForward()` | Move one grid step in the direction the jeepney faces. |
| `turnLeft()` / `turnRight()` | Rotate without moving. |
| `driveToNextStop()` | Pathfind to the nearest useful pickup/drop-off target, or to the terminal when no rider target remains. |
| `driveToTerminal()` | Pathfind to the current route terminal. |
| `driveToDestination()` | Backward-compatible alias for destination/terminal navigation. |
| `pickUp()` | Board waiting riders at the current stop. |
| `collectFare()` | Collect unpaid fares from riders aboard and record their tender. Returns the fare total. |
| `giveChange(amount)` | Give exact change for the active fare batch. Use `giveChange(changeOwed())`. |
| `dropOff()` | Let riders off at their requested stop, only after fare and change are settled. |
| `wait()` | Spend a tick doing nothing. |

## Queries

Queries return True or False and belong inside `if` or `while`.

| Query | True when... |
|---|---|
| `frontIsClear()` / `leftIsClear()` / `rightIsClear()` | That neighboring road cell is open. |
| `atStop()` | The jeepney is on a passenger stop. |
| `passengerWaiting()` | A rider is waiting at the current stop. |
| `hasPassengerAboard()` | At least one rider is aboard. |
| `atRequestedStop()` | Aboard riders want to get off here. |
| `isFull()` | No seats are available. |
| `routeComplete()` | All required riders are delivered, nobody required is waiting/aboard, and the jeepney is at the current terminal. |
| `atDestination()` | Maze/minigame-style destination check; procedural Automation should use `routeComplete()`. |

## Reporters

Reporters return values that can be assigned, printed, or passed into actions.

| Reporter | Value |
|---|---|
| `fareOwed()` | Total unpaid fare for riders aboard. |
| `cashTendered()` | Cash collected for the current unsettled fare batch. |
| `changeOwed()` | Exact change still owed. |
| `seatsLeft()` | Remaining passenger seats. |
| `passengerCount()` | Riders currently aboard. |
| `distanceToDestination()` | Grid distance to the terminal/destination. |
| `distanceTraveled()` | Steps used so far. |

## Procedural Route Pattern

```python
while not routeComplete():
    driveToNextStop()
    if passengerWaiting():
        pickUp()
        collectFare()
        giveChange(changeOwed())
    if atRequestedStop():
        dropOff()
```

## Notes

- `atDestination()` remains valid in maze and minigame content.
- Procedural Automation palettes should prefer `routeComplete()` and
  `driveToTerminal()`.
- Empty-fuel refueling is handled by the refuel minigame and charges the wallet;
  Gemma's tutorial drill is the one free instructional exception.
