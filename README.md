# CodinGame 2022 Spring Challenge Solutio

My humble version of C# Codingame Spring Challenge 2022 Solution. 

## Points of interest

This solution implements weighted decision-making system: any possible character's game action in a specific moment of time is associated with some numeric weight. 

Weight calculus is based on the current game state (hero coordinates, monsters coordinates, base health, mana left, current turn no and so forth) and on the predefined set of numbers, that describe current hero's personality:

```
double Attacker // tends to attack opponent's base
double Defender // tends to defend own base
double Duelist // tends to attacks opponent's heroes
double Hunter // tends to attack monsters
double Prodigal // tends to cast spells
double Conservator // tends to return to initial position
```

A set of these six numbers (i.e. "personality vector") is set during the creation of Hero class at the program start.

During eqach geme turn, a set of weights for every possible game decision is calculated for every hero. Then, "a hero makes a decision" - an action with maximal weight is performed.

In other words, I can't exactly know, which action specific hero will choose at a particular moment of the game. I've just had to setup three heros' sets of personality and look, how this team will handle game's challenges. 

Sample raplay: https://www.codingame.com/replay/622584657

To make weights to be comparable with each other, a simple normalization/mapping function is used, that maps values from interval (0; Infinity) to (0; 1) interval:

```
double InfiniteScalarsToWeight(double moreIsBetter, double lessIsBetter) // max 1, min 0
{
    return 1 - (1/((moreIsBetter * moreIsBetter) / (lessIsBetter * lessIsBetter) + 1));
}
```

It has two parameters: moreIsBetter for the product of values that positively influence making specific game decision, and lessIsBetter the product of values that negatively influence making specific game decision.

Each product may contain hero's characteristics (like `Attacker`, `Hunter` etc), as well as parts of current game state (health, mana, distances etc.)

## Platform inconvenience

On codingame.com platform during that challenge one haven't had an ability to access a history of matches' results (that, combined with vector of algo input parameters, like heroes' personality characteristics, could give an opportunity to optimize these set of parameters using some kind of AI like GA, NN or RBF).

One also haven't had an ability to start the fight simulation programmatically, only by hand.

These facts stopped me from further participations in this events: I've made the algo fully parametrized (so, ready to be optimized using some AI texhniques) - but ther were no convenient ability for that optimization provided.

Basically, Codingame platform design enforces me to write bot, not AI.

After spending a couple of hours optimizing the set of 18 floating point numbers manually (change -> press button -> collect logs -> restart), I've finally decided to leave this challenge. So

## Actual results

Were not so high. My bot (not AI, just pre-AI stub) reached 4166th place of 7695, ending the competition in Silver league (1,646th of 2,113 league members).

Nevertheless, it was a great, entertaining and challenging experience.

## Disclaimer

Code published as-is, without any polish after end of the event. Since it's the code from a time-limited challenge, there could be places where it's a little bit dirty, there's no comments or even some errors. Please accept these facts understandingly.

# If you benefit from my work

You're welcome to share a tiny part of your success with me:

[!["Buy Me A Coffee"](https://www.buymeacoffee.com/assets/img/custom_images/orange_img.png)](https://www.buymeacoffee.com/rextextaucom)
