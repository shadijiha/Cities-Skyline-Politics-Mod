# Politics & Elections Mod

Citizens vote, parties form coalitions, and the winning coalition actually changes your city.

This mod layers a parliamentary democracy on top of your Cities: Skylines 1 game. Your population holds elections at a configurable interval, votes based on their own demographics and grievances, and the ruling coalition enacts city policies, adjusts tax rates, and shifts service budgets. It is designed to make your city feel politically alive without taking control away from you.

> **Backup your save before using this mod.** The mod modifies vanilla state (taxes, budgets, city policies) and persists custom data in your savegame. Keeping a backup is strongly recommended, especially early in a session when you are still tuning parties and policies.

## Installation

1. Subscribe to **Harmony 2** (workshop ID 2040656402) by boformer. Required.
2. Subscribe to this mod.
3. Enable it in Content Manager -> Mods.
4. Load a save. The first election kicks off once your population clears the configured threshold.

## Features

- **Democratic elections** at a configurable term length, with campaign and re-election periods you can tune in-game.
- **Dynamic parliament size**, scaling with population (one seat per ~1000 citizens, rounded to an odd number so there are no ties).
- **Editable parties.** Create, rename, recolor, or delete political parties. Assign them an ideology, a budget platform, a tax platform, and a set of supported vanilla policies.
- **Native-style policy icons** inside the party editor. The list reflects whichever vanilla and DLC policies your game actually has.
- **Grievance-based voting.** Citizens with specific complaints (high taxes, poor health, unemployment, high crime) tilt their vote toward parties that address their grievance.
- **Voter trait sliders.** Nudge the economic leaning of every demographic group (education levels, wealth classes, employment status, age groups, sick citizens, pollution exposure).
- **Coalition formation** via greedy ideological matching. If no coalition can clear a majority, a snap re-election is called after a cooldown.
- **Automatic budget and tax changes** when a new coalition forms, scaled by which parties are in power.
- **Vanilla city policies** are auto-enacted or repealed by the coalition, mirroring their combined platforms.
- **Info-view overlay.** Clicking a Politics button puts the game into an info-view mode where residential buildings are tinted by the party their residents voted for. Cycle to Turnout and Satisfaction views.
- **Election Stats window** with horizontal bar charts for "why people voted" plus stacked bars for vote-by-age, vote-by-education, and vote-by-wealth.
- **Parliament hemicycle.** A real semi-circular parliament layout, ordered left to right by party ideology.
- **Chirper integration.** City News chirps for campaigns, results, bills, repeals, budget bills, and occasional citizen reactions. A "Minimize chirps" toggle strips it down to the essentials.
- **Deficit pressure.** When your city runs a weekly deficit, voters drift right and occasional citizen chirps complain about taxes and spending.
- **Everything persists** in the savegame: parties, voter traits, election history, runtime settings, and the chirp toggle.

## Quick start

- **Ctrl + P** opens the main Politics panel.
- Click **Call snap election** to force an early vote.
- Click **Manage Parties** to edit the political landscape.
- Click **Voter Traits** to shape how demographics lean.
- Click **Election Stats** to see who voted for whom and why.
- The **Politics: Off** button (top right) cycles the info-view overlay on and off.

## Known limitations

- The live vanilla Economy window and City Policies window may not visually refresh until you close and reopen them, even though the underlying state changes. The values persist and the simulation uses them immediately.
- Pollution, noise, and low-land-value grievances are currently disabled because the CS1 `Notification.Problem` enum varies across builds. The mod still reads crime, taxes, health, education, and employment.

---

## Nerd section: how it actually works

### Parliament size

```
seats = clamp( population / 1000, 25, 601 )
if seats is even, seats += 1
majority = (seats / 2) + 1
```

Evaluated every election, so a growing city gets a growing parliament.

### Voter decision

For each non-child, non-teen citizen sampled:

```
econ = sum of VoterTraits biases for (education, wealth, employment, age, sick)
     + pollution proxy (if home building is dense)
     + deficit pressure (0 to +0.35, grows per consecutive deficit week)
     + small jitter (~ +/- VoterNoise)

soc  = simple function of age and education
gov  = random jitter

ideologyFit(party) = 1 - distance(party.ideology, voterPoint) / sqrt(12)
grievanceMatch(party) = weighted sum of ScorePartyForGrievance(party, g) for g in citizen grievances

total(party) = 0.4 * ideologyFit + 0.5 * (grievanceMatch + 1) / 2 + 0.1 * noise
```

The citizen votes for the party with the highest `total`. Happy voters (health and wellbeing both at least 60) occasionally cast an incumbency bonus toward the ruling coalition.

### Grievances

Derived from live game state. Examples:

- **High taxes**: citizen's own residential tax is 12% or above, scaled to 1.0 around 20%+.
- **Poor health**: citizen's `m_health` under 50.
- **Unemployment**: young or adult citizen with no `m_workBuilding`.
- **Poor education**: uneducated adult.
- **High crime**: the district-level `m_finalCrimeRate` is above 15%.

Each grievance is scored against each party using the party's own budget deltas and platform. A "high taxes" citizen gets a positive score toward parties with negative tax deltas. A "poor health" citizen likes parties with big health budget boosts. A "high crime" citizen likes parties with a big police budget, and so on.

### Coalition formation

1. Sort parties by seat count, descending.
2. The largest party is the lead.
3. Greedily add the party whose ideology is closest to the running average of the coalition until the combined seat count crosses the majority threshold, capped at `MaxCoalitionPartners` (default 4).
4. If no feasible coalition exists, declare a failed formation and schedule a snap re-election after `ReElectionCooldownDays`.

### Seat allocation

The largest-remainders method. Each party's exact quota is `voteShare * totalSeats`. Integer seats are floored, then remaining seats are given to parties with the largest fractional quotas.

### Applying the coalition

- Combined **tax deltas** and **budget deltas** across all coalition parties are summed and applied via `EconomyManager.SetTaxRate` and `SetBudget`, marshaled onto the simulation thread to avoid threading errors.
- The union of all coalition parties' **supported vanilla policies** is set via `DistrictManager.SetCityPolicy`, also on the simulation thread.
- Any **previous** coalition policy that the new coalition does not support is repealed with its own chirp.

### Bill voting tally

When a policy is enacted or repealed, the chirp shows a simulated YES-NO tally:

- Parties whose platform includes the bill: 95% of their seats vote yes.
- Other coalition parties: 80% yes (loyalty).
- Opposition: yes-share = `clamp(0.55 - 0.18 * ideologyDistance, 0.05, 0.6)` based on distance to the bill's supporters.
- Each party has a small abstention fraction.

If the arithmetic would have produced a rejection despite the bill having actually been enacted, the numbers are adjusted so the chirp narrative still reads "passes". The bill really did pass in the simulation.

### Info-view overlay

Harmony patches `BuildingAI.GetColor` (base and every overriding subclass) so residential buildings are tinted by their dominant voting party when the info view is active. The mod piggybacks on the vanilla Density info mode so the standard legend panel and dimming still appear.

### Why the demographic charts need at least one election after installing

The demographic cross-tabs (vote by age, education, wealth) are captured during the election sample. Results from before the feature was added do not have them and will display "no data". Any election held after you update the mod will populate them.

### Deficit pressure

Every 7 in-game days the mod samples `EconomyManager.LastCashAmount` and compares to the previous sample. If income dropped, `DeficitWeeks` increments, otherwise it resets. The current pressure is:

```
DeficitPressure = DeficitWeeks == 0 ? 0 : 0.05 + 0.30 * clamp(DeficitWeeks / 6, 0, 1)
```

This is added to every voter's economic axis before the ideology distance calculation. At six straight weeks of deficit, every voter gets a +0.35 nudge to the right. It is enough to shift undecideds, not enough to override strong ideological or grievance-based voters.
