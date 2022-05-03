# CodinGame 2022 Spring Challenge Solutio

My humble version of C# Codingame Spring Challenge 2022 Solution. 

## Points of interest

This solution implements weighted decision-making system: any possible character's game action in a specific moment of time is associated with some numeric weight. Weight calculus is based on the current game state (hero coordinates, monsters coordinates, base health, mana left, current turn no and so forth) and on the predefined set of numbers, that describe current hero's personality:

```
public double Attacker { get; private set; } // tends to attack opponent's base
public double Defender { get; private set; } // tends to defend own base
public double Duelist { get; private set; } // tends to attacks opponent's heroes
public double Hunter { get; private set; } // tends to attack monsters
public double Prodigal { get; private set; } // tends to cast spells
public double Conservator { get; private set; } // tends to return to initial position
```

...

## Platform inconvenience

...

## Actual results

Not so high. My bot (not AI, just pre-AI stub) reached 4166th place of 7695, ending the competition in Silver league (1,646th of 2,113 league members).

Nevertheless, it was a great, entertaining and challenging experience.

## Disclaimer

Code published as-is, without any polish after end of the event. Since it's the code from a time-limited challenge, there could be places where it's a little bit dirty, there's no comments or even some errors. Please accept these facts understandingly.

# If you benefit from my work

You're welcome to share a tiny part of your success with me:

[!["Buy Me A Coffee"](https://www.buymeacoffee.com/assets/img/custom_images/orange_img.png)](https://www.buymeacoffee.com/rextextaucom)
