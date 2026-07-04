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
| `storyDropoffArmed()` | The front-seat story passenger's drop-off is armed and still undelivered. |

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
| `position()` | Current cell as a pair `(x, y)`. |
| `facing()` | Current heading: `0` = North, `1` = East, `2` = South, `3` = West. |
| `storyDropoffPosition()` | Story passenger's drop-off cell `(x, y)`, or `None` if not armed. |
| `nearestStopPosition()` | Nearest useful stop `(x, y)`, or `None` when no rider target remains. |
| `destinationPosition()` | The route terminal/destination cell `(x, y)`. |
| `directionTo(x, y)` | Heading of the first step along the shortest path to `(x, y)`, or `None`. |
| `distanceTo(x, y)` | Shortest walkable distance from the jeepney to `(x, y)`. |

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

## Building your own `driveToDropoff()`

The autopilot demonstrates how the built-in `driveToDropoff()` can be rebuilt from lower-level navigation sensors. The same primitives let you write your own path-following algorithms:

```python
def driveToDropoff():
    if storyDropoffArmed():
        navigateTo(storyDropoffPosition(), 4)
    else:
        target = nearestStopPosition()
        if target == None:
            target = destinationPosition()
        navigateTo(target, 4)

def navigateTo(target, limit):
    steps = 0
    while distanceTo(target[0], target[1]) > 0 and steps < limit:
        want = directionTo(target[0], target[1])
        if want == None:
            break
        turnTo(want)
        moveForward()
        steps = steps + 1

def turnTo(want):
    if want == facing():
        return
    if (facing() + 1) % 4 == want:
        turnRight()
    elif (facing() + 2) % 4 == want:
        turnRight()
        turnRight()
    else:
        turnLeft()
```

## Notes

- `atDestination()` remains valid in maze and minigame content.
- Procedural Automation palettes should prefer `routeComplete()` and
  `driveToTerminal()`.
- Empty-fuel refueling is handled by the refuel minigame and charges the wallet;
  Gemma's tutorial drill is the one free instructional exception.
