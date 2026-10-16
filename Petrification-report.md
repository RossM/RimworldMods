# Petrification: player experience and balance report

Analysis date: 2026-10-16. Repository snapshot: `e2908c746fe07fc5b4e55c1b31047e727a54f43a`. Installed game inspected: RimWorld **1.7.4980 rev60**.

This report describes the current implementation. It combines source inspection, analytical calculations, and simulations; it is not a record of in-game playtesting. Numerical results are rounded estimates under the assumptions in Appendix B. It documents existing behavior and the tradeoffs available to future tuning.

## Key findings

- **Medicine quality determines the long-term course.** Herbal care slows deterioration but cannot stop it. Prompt industrial treatment nearly stabilizes direct disease severity. Competent glitterworld treatment can reduce severity, without curing the disease or removing existing growths.
- **There are two separate problems to manage:** systemic disease severity and persistent petrified body parts. Making the disease dormant solves neither accumulated problem.
- **Direct doctor work is modest, but surgery matters.** Fetching medicine, scheduling operations, postoperative impairment, and failed operations add burdens beyond routine tending. A pawn can suffer serious impairment well before reaching either systemic death or full petrification of a part.
- **The 40% disease boundary is the main escalation point.** It triples growth frequency and increases average growth size, producing about **6.43 times as much petrified tissue per active day**.
- **Full petrification within one treated episode is rare for a healthy adult Titan.** Repeated surgery exposes the pawn to complications much more often. Existing wounds, old growths, bionics, and delayed care can materially change the comparison.
- **Several tuning controls are discontinuous.** Treatment-count thresholds, whole medicine units, and rounded body-part HP can make very small numerical changes produce unexpectedly large effects.

## Player perspective

### Managing an active episode

The disease begins active at 10% severity. Each episode rolls a progression rate of 0.8-1.2 percentage points per day and keeps that rate until remission. While treatment is in effect, a 70% tend subtracts 0.7 percentage points per day from progression, for example.

Each tend also contributes its quality toward a cumulative **300% quality requirement**. Reaching that requirement immediately makes the disease dormant. Active episodes do not spontaneously end without treatment. The final tend counts toward remission but does not subsequently provide five days of severity reduction: remission resets tend quality to zero.

For a healthy Medicine-8 doctor, a regular bed, and prompt treatment:

| Disease medicine | Mean active episode | Mean direct severity change |
|---|---:|---:|
| Herbal | 24.0 days | +10.3 percentage points |
| Industrial | 14.6 days | +0.95 percentage points |
| Glitterworld | 10.1 days | -2.40 percentage points |

These are averages, not guarantees. Progression rate and tend quality vary. In particular, a doctor's displayed 100% Medical tend quality does not produce constant 100% industrial tends: negative quality rolls lower the result, while positive rolls are capped. Actual mean quality at that ceiling is 93.75%.

Prompt industrial care is therefore close to stable, with slow positive average drift. Herbal care consumes substantial remaining severity headroom with each episode. Glitterworld medicine needs a competent doctor; with a Medicine-4 doctor it still produces slightly positive average drift.

Without any tending, the first episode reaches 100% systemic severity after **75-112.5 days**, averaging about 91 days across progression rolls. Body-part effects can threaten the pawn independently of that deadline.

### Dormancy is a respite, not recovery

Dormancy stops systemic progression, direct disease pain, and new growths. It does not reduce systemic severity or heal petrified flesh. Existing injuries and growth-related impairment remain.

Reactivation is random, with a mean dormant interval of **120 days**, or two 60-day RimWorld years. The median is about 83 days; this is not a scheduled two-year recurrence. Roughly 10% of dormant intervals are shorter than 13 days and 10% longer than 276 days.

Softener prevents the disease from developing. It has no effect on an established disease, including one currently dormant.

### Treatment punctuality matters

At the industrial quality ceiling, delaying the first tend and each subsequent appointment by one day increases mean episode severity gain from about **+0.95 to +4.48 points**. Mean active duration rises from 14.6 to 18.2 days. More time active also means more opportunities for growths.

The practical requirement is access to timely care rather than a large fraction of a doctor's working year. Routine disease tending alone averages about **0.47 doctor-hours per year** for the reference Medicine-8 doctor using industrial medicine, after including dormancy. The initial episode requires its treatment course immediately; this average is not an evenly distributed workload.

### Treating petrified flesh

Growths reduce a part's efficiency in proportion to petrified severity divided by its maximum HP. They do not directly subtract injury HP. At full petrification, the efficiency penalty reaches 100%, and excision is unavailable.

Excision removes one part's accumulated petrification, replacing it with an equally severe surgical cut that is immediately tended. The cut's tend is permanent, so successful excision does not create scheduled retending work. The wound still needs to heal, and Titans have slow wound healing.

Excision is available only while petrified severity is **strictly less than remaining injury-adjusted part HP**. Existing wounds can therefore temporarily block an operation before the part is fully petrified. Healing those wounds may reopen excision access if the growth itself remains below maximum part HP. Vital-part impairment can be dangerous before that recovery occurs.

Each operation requires two medicine and 2,000 work: about **55 in-game minutes** for a healthy Medicine-8 surgeon. If each growth is excised separately, an industrial-treated episode needs about **one successful excision at minor severity**, or **three at major severity**. Using industrial medicine for both disease care and surgery, and a 100% combined bed/room surgery factor, approximate total direct doctor work is:

- **2.2 hours per minor-stage episode**, including disease tending and expected surgery retries.
- **4.4 hours per major-stage episode** under the same conditions.

These estimates omit travel, medicine collection, treating failure injuries, and waits for recovery. Operations can use better medicine than routine disease tending.

Waiting until remission lets repeat growths on the same part share one operation. In the reference scenarios this saves about **2-12% of operations**, at the cost of longer impairment and more accumulation before removal. The value of waiting depends on the affected part, existing growths, and other injuries.

### Body-part loss and bionics

A healthy adult Titan's smallest parts have 11 HP, while the largest possible single growth has severity 10. A single growth therefore cannot fully petrify any healthy adult Titan part. Repeat hits are required.

Starting with clear parts, the growth-only chance of any part becoming fully petrified during an industrial-treated, entirely major-stage episode is about **0.083%** if no growths are removed until remission. For a herbal-treated major-stage episode it is about **0.26%**. At minor severity the corresponding risks are extremely small. These figures concern full petrification, not partial disability, surgical complications, or overall mortality.

Leaving growths through multiple episodes changes the situation: dormancy does not reset accumulated exposure. With growths never removed and the disease held at major severity, mean time to the first fully petrified part is about **158 cumulative active days**. This is not a patient lifespan estimate.

Bionic and other artificial replacements cannot receive new growths, and their descendant parts are excluded too. Normal installation removes existing growths from the replaced part and its descendants. Replacements do not cure the systemic disease or reduce growth-event frequency. Remaining eligible parts receive a larger share of events, so protecting particular parts does not imply a proportional reduction in overall risk or workload.

## Game balance perspective

### The disease rewards infrastructure and reliability

The current medicine tiers create distinct outcomes: herbal care defers deterioration, industrial care nearly balances it, and competent glitterworld care can reverse systemic severity. Better doctors improve quality until medicine caps intervene; beyond those caps they continue to improve work speed and surgical reliability.

The recurring direct workload is small compared with a full working year, particularly during long dormant intervals. Its burden comes from interruptions, medicine supply, operation scheduling, postoperative impairment, and complications. Doctor-hours alone understate the player experience. Conversely, an average calendar cost can obscure the concentrated demands of a new active episode.

### Early severity control has a substantial downstream benefit

Below 40%, growths arrive every 15 active days on average and add mean severity 3.5. At or above 40%, they arrive every five days and add mean severity 7.5. This creates a strong escalation in tissue damage and a smaller, but still substantial, escalation in surgery count.

Progression and suppression tuning therefore affect more than systemic death. They determine how soon the pawn enters a more expensive and damaging tissue regime. Raising the 80% boundary has a narrower effect: the extreme stage increases direct disease pain, but currently uses the same growth frequency and sizes as the major stage.

### Surgical exposure can dominate the risks of regular maintenance

The excision recipe inherits ordinary surgery outcomes and the 98% success ceiling. For a 14.6-day major-stage episode with separate removal of each growth, the chance of at least one failed operation is approximately **5.7% even at that ceiling**, compared with a **0.083%** growth-only full-petrification risk when waiting until remission.

With a Medicine-8 surgeon, industrial medicine, and a 100% bed/room factor, success is 80% per operation. The same episode then has about a **44% chance of at least one failed operation**. Failure does not mean losing a part: it can cause recoverable wounds, permanent damage, or destruction depending on the injury process and pawn state. This analysis does not convert failure frequency into a reliable part-loss or mortality rate.

Maintenance therefore exchanges persistent impairment and accumulation risk for surgical exposure. Increasing growth frequency increases both sides of that trade. Increasing growth size increases impairment and wound severity without necessarily increasing the number of operations.

### Tuning controls have different roles

| Balance intent | Principal numerical controls | Important coupling |
|---|---|---|
| Change which level of care can stabilize systemic severity | Progression range; suppression coefficient | Near industrial balance, small changes can reverse the sign of drift; stage-entry timing also changes. |
| Change active episode length and treatment visits | Cumulative remission quality; tend duration | Quality thresholds create discrete treatment-count changes; longer intervals give strong treatment more time to reduce severity but permit more growths. |
| Change recurrence and calendar burden | Dormant MTB | Changes recurring rates without changing an individual episode or clearing old damage. |
| Change operation frequency and associated surgical exposure | Growth MTB | Also changes tissue production and repeated-hit risk. |
| Change damage per growth and recovery burden | Growth severity range | Operation count is largely unchanged under one-operation-per-growth care. |
| Change functional impairment without changing growth amount | Flesh efficiency coefficient | Surgical cuts still equal growth severity; excision eligibility still depends on HP. |
| Change direct operation labor or resource cost | Surgery work; medicine count | Work is approximately linear; whole medicine requirements round upward. |

### Thresholds require deliberate handling

The 300% remission requirement is located exactly on the total of three capped industrial tends. Any increase above it removes the three-tend outcome for those episodes, while a 1% increase does nothing to the reference glitterworld episode's treatment count. This is a real discontinuity, not simulation noise.

Several other parameters also resist smooth tuning. Medicine requirements round upward to whole items, and body-part HP is rounded upward after health scaling. Lethal severity must remain reachable under the systemic severity cap. Meanwhile, the flesh stage boundaries currently change labels and threat classification without changing their shared efficiency coefficient; its nominal maximum severity is also locally inactive below the per-part HP cap.

These distinctions matter when choosing a control: a change that seems numerically small may cross a discrete boundary, while another apparently meaningful change may have no physical effect.

## Appendix A: tuning parameters and game constants

### A.1 Systemic disease

Values are from `XylPetrification` in [GeneDefs_Petrification.xml](XylXenos.Data/Defs/GeneDefs/GeneDefs_Petrification.xml).

| Parameter | Current value | Interpretation |
|---|---:|---|
| `initialSeverity` | 0.10 | New disease starts at 10%. |
| `minSeverity` | 0.01 | Severity cannot fall below 1%. |
| `maxSeverity` | 1.00 | Systemic severity cap. |
| `lethalSeverity` | 1.00 | Systemic death threshold. |
| Active `severityPerDayRange` | 0.008-0.012 | Uniform episode roll: 0.8-1.2 percentage points/day. |
| Active `changeMtbDays` | -1, inherited default | No spontaneous mode change. A positive value would introduce a different remission mechanism. |
| Dormant progression | 0 | No systemic growth while dormant. |
| Dormant `changeMtbDays` | 120 days | Random reactivation with this mean interval. |
| `baseTendDurationHours` | 120 hours | Five-day base treatment interval. |
| `severityPerDayTended` | -0.01 | Subtracts one percentage point/day at 100% quality. |
| `changeModeAtTotalTendQuality` | 3.00 | Remission after cumulative quality reaches 300%. |
| Mode selection weights | 1 each | With only two modes, changing these weights does not change which mode follows a transition. |

| Systemic stage | Severity range | Direct pain offset | Growth MTB | Growth severity |
|---|---|---:|---:|---:|
| Minor | Below 40% | 0.10 | 15 days | Uniform 2-5 |
| Major | 40% to below 80% | 0.20 | 5 days | Uniform 5-10 |
| Extreme | 80% or above | 0.40 | 5 days | Uniform 5-10 |

Dormancy disables direct disease pain, stage hediff givers, and further tending. Growths can stack on an already affected part. Each growth event selects one eligible live part, weighted by `coverageAbs`, excluding parts with a directly added replacement on themselves or an ancestor.

### A.2 Petrified flesh and surgery

| Parameter or rule | Current value |
|---|---|
| Relative flesh severity | Petrified severity / maximum part HP |
| Flesh severity cap | Lesser of requested severity and maximum part HP, also subject to the hediff's nominal cap |
| Nominal flesh `maxSeverity` | 100; above ordinary Titan part HP |
| Flesh stage boundaries | Relative severity 0.40, 0.80, 1.00 |
| `partEfficiencyOffset` | -1.00 in every stage, multiplied by relative severity |
| Excision eligibility | Growth visible, on a part, and severity strictly below remaining part HP |
| Excision `workAmount` | 2,000 |
| Excision medicine requirement | 2 units of any allowed medicine |
| Excision surgery success factor | 1.00, inherited default |
| Excision surgery outcomes | Inherited `SurgeryOutcomeBase` |
| Replacement wound | `SurgicalCut`, severity equal to removed growth, tended immediately |
| Removed material | One limestone chunk when removed by a surgeon through this recipe |

The successful-excision worker directly creates its wound; the analysis does not assign the random damage-worker scarring process to that successful cut. Failed surgery uses ordinary surgery-injury mechanics and can introduce additional damage and scars.

### A.3 Titan and vanilla treatment constants

The standard Titan has the giant gene's **1.3 health-scale factor** and slow wound healing's **0.5 injury-healing factor**. Maximum part HP is `ceil(base HP * health scale)`. The body-size factor is 1.5, but petrification growth sizes are not scaled by body size or HP.

| Example part | Base HP | Adult Titan HP | Growth-event targeting share, before replacements |
|---|---:|---:|---:|
| Finger/toe | 8 | 11 | Varies by digit; individually small |
| Brain | 10 | 13 | 0.864% |
| Each eye | 10 | 13 | 0.420% |
| Heart | 15 | 20 | 2.000% |
| Each arm | 30 | 39 | 6.0984% |
| Each leg | 30 | 39 | 9.800% |
| Torso | 40 | 52 | 15.000% |

The installed vanilla human body was used, including bones marked alive. Part targeting weights sum to one before exclusions; this is not a uniform draw over part names.

| Treatment | Medical potency | Maximum tend quality |
|---|---:|---:|
| No medicine | 0.30 | 0.70 |
| Herbal | 0.60 | 0.70 |
| Industrial | 1.00 | 1.00 |
| Glitterworld | 1.60 | 1.30 |

For ordinary tending, the game first computes base quality from the doctor's Medical tend quality stat, potency, bed offset, and self-tend adjustment, then clamps it to the medicine ceiling. It subsequently adds a uniform random -0.25 to +0.25 and clamps again. Self-tending has a 0.7 factor; no self-tending was modeled.

| Reference doctor | Medical tend quality stat | Tend/operation speed | Base surgery success stat |
|---|---:|---:|---:|
| Medicine 4 | 0.60 | 0.64 | 0.50 |
| Medicine 8 | 1.00 | 0.88 | 0.80 |
| Medicine 14 | 1.30 | 1.24 | 0.98 |

These assume normal sight, manipulation, and global work speed. Base tending work is 600 ticks. One game hour is 2,500 ticks; one day is 60,000 ticks; one year is 60 days. Treatment has an inherited three-hour overlap. With earliest retending and carried-over treatment time, the first interval is approximately five days and later intervals 5.125 days.

For surgery, medicine success multipliers are 0.88 for herbal, 1.00 for industrial, and 1.18 for glitterworld. Multiply these by the surgeon's success stat, bed/room factor, and recipe factor, then apply the 98% ceiling. All illustrative surgery tables use a combined bed/room factor of 1.00, no inspiration, and one medicine type per operation.

The custom excision worker uses medicine potency rather than `MedicalQualityMax` as the cap for its automatic postoperative tend. This differs from ordinary tending. Postoperative healing quality was not used to calculate the operation counts or labor tables.

### A.4 Prevention and incidence scope

Softener's defined schedule is every five days and its purpose is prevention before disease onset. This report conditions on an established disease. It does not estimate prevention-policy costs, incident frequency, gene metabolic value, or population-wide disease burden.

For reference, the genetic-disease incident has base chance 0.5, earliest day 10, minimum refire interval 30 days, minimum population 4, maximum one victim, and candidate chance factor 0.2 per eligible pawn, clamped at one. These are storyteller controls, not a direct per-pawn infection MTB, and were not used in the episode calculations.

## Appendix B: assumptions and calculation methods

### B.1 Episode model

1. The first tend is immediate, with no residual treatment timer from a preceding episode. Later tends occur at earliest eligibility unless a delay scenario is explicitly stated.
2. Disease progression is one independent uniform roll per active episode. Tend-quality rolls are independent and use the installed game's clamp-before-and-after-randomness behavior.
3. The general episode table ignores death truncation and the 1% severity floor. It describes potential episode duration and drift. Poor-care results can exceed a living pawn's remaining headroom; negative drift overstates improvement when already near the floor.
4. Routine doctor-hour estimates exclude travel, medicine collection, patient handling, skill changes, and interruptions. They use the reference doctor's work speeds.
5. Annual figures use a renewal approximation: one modeled active episode plus a mean 120-day dormant interval. They do not model rare early reactivations while the final tend's timer still blocks a new appointment.
6. Systemic care simulations used 300,000 episodes per doctor/medicine combination. The remission-threshold comparison used 2,000,000 shared-quality-sequence samples per medicine, with analytical cross-checks for industrial and glitterworld results. Delay and flesh simulations used 300,000 samples per scenario. Results are rounded to avoid implying gameplay-level precision.

Let quality `q` be a fraction, so 100% is 1. Let `d = 5` days, overlap `o = 0.125` days, and remission threshold `C = 3`. The number of tends is the first `N` for which the sum of qualities reaches `C`.

```text
T = (N - 1) * (d + o) - o
A = sum(q[i] * interval[i], for all tends before the final tend)
direct severity change = r * T - k * A
```

In percentage-point units, current mean `r = 1` point/day and `k = 1` point/day per unit quality. The final tend contributes to `C` but not to `A`.

At the industrial ceiling, quality equals one with probability one half and otherwise lies uniformly between 0.75 and one. Three tends end the episode only if all are capped: probability 1/8. Four always suffice. Consequently, mean `N = 3.875`, mean `T = 14.609375`, mean `A = 13.65625`, and mean drift is exactly 0.953125 percentage points in this model.

At the glitterworld ceiling, qualities lie between 1.05 and 1.30, with mean 1.2375. Three always suffice, giving `T = 10.125` days and mean drift -2.4046875 points.

### B.2 Growth and body-part model

Growth arrivals are approximated as a Poisson process with rate `lambda = 1 / mtbDays`. This neglects negligible interval discretization. A selected part receives a uniform growth-size roll; growths stack and cap at that part's maximum HP. Fully petrified parts remain eligible for later events.

For stage-held results, disease severity stays in the named stage. For the initial-episode flesh simulations, disease severity starts at 10% or 50%, can cross stage boundaries, and stops at remission or systemic death. The untreated calculation follows progression from 10% to 100%, including the 40% growth-regime change.

The body starts without growths, other injuries, missing parts, or replacements. No growths are excised during the full-petrification risk window. Thus the risk tables describe accumulation before cleanup, not a colony continuously operating on each new growth. They do not model partial-capacity death before the full-petrification threshold, healing wounds, or the detailed surgery damage process.

For a window of length `T`, each part's event count is Poisson with mean `lambda * T * partWeight`. Its accumulated severity is a compound Poisson sum of uniform growth rolls. The probability that any part reaches its HP threshold is one minus the product of part-survival probabilities. Fixed-window rare risks were calculated from these distributions rather than interpreting zero simulation observations as zero probability.

Expected distinct affected parts are:

```text
sum(1 - exp(-lambda * T * partWeight))
```

The mean time to first full petrification was simulated to the first threshold crossing. It excludes calendar dormancy and systemic death. It is a growth-only exposure measure, not mean time between independent losses or a forecast of actual lifespan.

### B.3 Surgery and workload model

Separate cleanup assumes one successful operation for each growth. End-of-episode cleanup uses distinct affected, still-operable parts. The models do not schedule a colony's actual medical jobs or simulate growths arriving while a cut is healing.

```text
routine tend hours = N * 600 / tendSpeed / 2500
successful-operation hours = operationCount * 2000 / operationSpeed / 2500
expected attempts, if repeatable = required successes / surgerySuccessProbability
calendar rate per day = expected per-cycle quantity / (120 + mean active days)
```

Retry-adjusted labor and medicine are idealized estimates. Failed operations may create injuries that require treatment, obstruct another operation, or change later risks; these extra effects are not costed. A failed-operation probability must not be read as a missing-part, scar, or death probability.

### B.4 Sensitivity model

Unless stated otherwise, a 1% perturbation is relative, not one percentage point. Sensitivities hold care quality, staffing availability, stage, and other parameters fixed, and ignore mortality/floor clipping. They describe local responses; they are not global lifespan derivatives.

Frequency scales growth count and severity production; size scales severity production but not event count. Calendar effects use active fraction `f = T / (120 + T)`. Persistent flesh does not stop impairing the pawn during dormancy even though production of new flesh stops.

The models omit doctor-capacity bottlenecks. If more work creates longer queues, the resulting treatment delays and accumulation can amplify otherwise small parameter changes.

## Appendix C: detailed results

### C.1 Systemic episode duration, drift, and routine tending

Work is in-game doctor-hours per episode, excluding excision. Poor-care rows are potential episodes without death truncation or severity-floor clipping.

| Medicine skill | Treatment | Mean actual tend quality | Tends | Active days | Severity change, pp | Doctor-hours |
|---|---|---:|---:|---:|---:|---:|
| 4 | None | 18.5% | 17.00 | 81.9 | +67.3 | 6.38 |
| 4 | Herbal | 36.0% | 8.91 | 40.4 | +26.2 | 3.34 |
| 4 | Industrial | 60.0% | 5.53 | 23.1 | +9.4 | 2.07 |
| 4 | Glitterworld | 96.0% | 3.68 | 13.6 | +0.7 | 1.38 |
| 8 | None | 30.0% | 10.62 | 49.2 | +34.8 | 2.90 |
| 8 | Herbal | 57.7% | 5.71 | 24.0 | +10.3 | 1.56 |
| 8 | Industrial | 93.75% | 3.875 | 14.609 | +0.953 | 1.06 |
| 8 | Glitterworld | 123.75% | 3.00 | 10.125 | -2.405 | 0.82 |
| 14 | None | 39.0% | 8.26 | 37.1 | +22.9 | 1.60 |
| 14 | Herbal | 63.75% | 5.15 | 21.2 | +7.7 | 1.00 |
| 14 | Industrial | 93.75% | 3.875 | 14.609 | +0.953 | 0.75 |
| 14 | Glitterworld | 123.75% | 3.00 | 10.125 | -2.405 | 0.58 |

For comparison, ideal constant-quality tends, ignoring numerical boundary artifacts:

| Quality every time | Tends | Active days | Mean severity change, pp |
|---|---:|---:|---:|
| 50% | 6 | 25.50 | +12.75 |
| 70% | 5 | 20.375 | +6.11 |
| 90% | 4 | 15.25 | +1.53 |
| 100% | 3 | 10.125 | 0 |
| 120% | 3 | 10.125 | -2.03 |

Constant 100% quality gives zero mean drift, but individual progression rolls still produce approximately -2.03 to +2.03 points per episode. Constant 120% quality counters even the fastest progression roll while continuously treated.

### C.2 Appointment delay and annual routine care

Industrial quality ceiling; delay is applied before the first tend and after each subsequent appointment becomes eligible:

| Delay | Mean active days | Mean severity change, pp |
|---|---:|---:|
| None | 14.6 | +0.95 |
| 6 hours | 15.3 | +1.56 |
| 1 day | 18.2 | +4.48 |
| 2 days | 22.1 | +8.34 |

Medicine-8 doctor, prompt care, including mean dormancy, excluding surgery:

| Disease treatment | Tends per 60-day year | Direct doctor-hours/year | Medicine units/year |
|---|---:|---:|---:|
| None | 3.77 | 1.03 | 0 |
| Herbal | 2.38 | 0.65 | 2.38 |
| Industrial | 1.73 | 0.47 | 1.73 |
| Glitterworld | 1.38 | 0.38 | 1.38 |

### C.3 Flesh cleanup per episode

Medicine-8 doctor. Minor scenarios start at 10% systemic severity; major scenarios start at 50%. The medicine column determines disease treatment and, in the retry estimates, surgery medicine too. Expected successful operations equal growth events under separate cleanup. End cleanup counts each affected operable part once.

| Stage scenario | Medicine | Separate successful operations | End-cleanup operations | Surgery hours before failures | Surgery hours with idealized retries | Medicine for surgery with retries |
|---|---|---:|---:|---:|---:|---:|
| Minor | Herbal | 1.60 | 1.53 | 1.45 | 2.07 | 4.55 |
| Minor | Industrial | 0.97 | 0.95 | 0.89 | 1.11 | 2.44 |
| Minor | Glitterworld | 0.67 | 0.66 | 0.61 | 0.65 | 1.43 |
| Major | Herbal | 4.81 | 4.22 | 4.37 | 6.21 | 13.66 |
| Major | Industrial | 2.92 | 2.69 | 2.65 | 3.32 | 7.30 |
| Major | Glitterworld | 2.03 | 1.91 | 1.84 | 1.95 | 4.29 |

Retry columns use separate cleanup and surgery success probabilities 0.704, 0.80, and 0.944 respectively. They exclude treatment of failure injuries and any loss of retry access. Add routine disease-tending work from C.1 to obtain total direct doctor-hours.

A fresh 10% disease treated without medicine generates about **4.37 growth events** before remission on average in the stage-aware model. The simpler all-minor estimate is only 3.28: some such episodes cross 40% and accelerate growth. This distinction matters when applying stage-held estimates to poor care.

### C.4 Full-petrification risk without excision

Clear, fully organic, uninjured adult Titan; stage held constant. Percentages concern at least one part reaching its maximum petrified severity.

| Cumulative active days | Minor-stage probability | Major-stage probability |
|---|---:|---:|
| 10.125 | 0.000000302% | 0.0370% |
| 14.609 | 0.000000913% | 0.0828% |
| 24 | 0.00000410% | 0.260% |
| 40 | 0.0000195% | 0.949% |
| 60 | 0.0000684% | 3.02% |
| 120 | 0.000632% | 25.7% |
| 360 | 0.0419% | 99.94% |

| Stage held constant | Mean active days to first full part | Median | 10th-90th percentiles |
|---|---:|---:|---:|
| Minor | 1,248 | 1,242 | 888-1,617 |
| Major/extreme | 158 | 155 | 88-230 |

Torso and legs account for approximately 86.6% of first full-petrification outcomes in the minor model and 62.8% in the major model. Their larger targeting weights outweigh the protection of greater HP. These are shares of first full outcomes, not per-event chances of losing those parts.

With no disease tending and no excision, following progression from 10% to 100% gives about **14.2 growth events** and a **3.8% chance of some fully petrified part before reaching 100% systemic severity**. Partial-capacity deaths are not modeled.

### C.5 Surgical complication exposure

Healthy surgeons, combined bed/room factor 1.00:

| Medicine skill | Herbal success | Industrial success | Glitterworld success |
|---|---:|---:|---:|
| 4 | 44.0% | 50.0% | 59.0% |
| 8 | 70.4% | 80.0% | 94.4% |
| 14 | 86.24% | 98.0% | 98.0% |

For a fixed 14.609-day episode, one successful removal per growth:

| Stage | Any failed operation at 98% success | Any failed operation at 80% success |
|---|---:|---:|
| Minor | 1.93% | 17.7% |
| Major | 5.68% | 44.3% |

The no-failure probability is `exp(-lambda * T * (1 - p))`. Failure frequency while repeatedly attempting cleanup is approximately `lambda * (1 - p) / p`. At 98% success this means one failed operation per **735 minor-stage active days** or **245 major-stage active days**, on average. These are failed-operation intervals, not body-part-loss intervals.

## Appendix D: numerical tuning sensitivities

### D.1 Direct severity and timing

Change in mean severity points per episode following a 1% relative parameter increase, with stage held fixed and no clipping:

| Medicine | Baseline drift, pp | Progression rate +1% | Suppression magnitude +1% | Base tend duration +1% |
|---|---:|---:|---:|---:|
| Herbal | +10.27 | +0.240 | -0.137 | +0.101 |
| Industrial | +0.953 | +0.146 | -0.137 | +0.0094 |
| Glitterworld | -2.405 | +0.101 | -0.125 | -0.0238 |

For industrial care, the direct-drift elasticities are about +15.3 for progression and -14.3 for suppression. Reducing mean progression by 6.52%, or increasing suppression by 6.98%, makes average direct drift zero in this model. This does not establish overall survival or clear existing flesh.

Increasing only the lower progression bound by 1% raises the midpoint by 0.004 percentage points/day; increasing only the upper bound by 1% raises it by 0.006 points/day. Multiply these changes by mean episode duration for the corresponding direct severity effect. A symmetric spread increase around an unchanged midpoint affects variation rather than mean episode drift before clipping or stage effects.

At the industrial baseline, a 1% increase in base tend duration produces approximately +0.984% active duration, +0.877% calendar growth/excision demand, and -0.107% calendar routine tends. Total doctor-work sensitivity depends on the mix of routine tends and operations.

A 1% increase in dormant MTB reduces calendar production/work rates by approximately 0.83%, 0.89%, and 0.92% for herbal, industrial, and glitterworld care respectively. Per-episode results and the initial episode are unchanged.

### D.2 Remission threshold

| Medicine | Active days at threshold 2.97 | At 3.00 | At 3.03 |
|---|---:|---:|---:|
| Herbal | 23.74 | 24.01 | 24.29 |
| Industrial | 14.36 | 14.61 | 15.25 |
| Glitterworld | 10.125 | 10.125 | 10.125 |

Immediately above three, industrial care loses its three-perfect-tend remission outcome: a jump of 0.125 expected tends and 0.640625 expected active days. A 1% threshold increase gives approximately +3.23% tends/episode, +4.39% active time/episode, +2.74% calendar routine tends, and +3.89% calendar growths. Mean direct drift barely changes because the added interval follows a perfect tend.

The lack of a glitterworld response within this range is a treatment-count plateau. First-order derivatives at these boundaries cannot be inferred from a smooth average-quality approximation.

### D.3 Flesh, surgery, and discrete parameters

| Parameter change | Local response, before thresholds or queue effects |
|---|---|
| Growth MTB +1% | About -1% events, tissue production, separate operations, and surgical exposure. Mean growth-only time to first full part increases 1%. |
| Both growth-size bounds +1% | +1% tissue production and excision wound amount; approximately unchanged operation count. |
| Flesh efficiency penalty magnitude +1% | +1% efficiency penalty before clipping; unchanged event count, cut severity, and HP eligibility check. |
| Surgery work +1% | +1% direct surgery hours; unchanged inherent failure probability and medicine requirement. |
| Stage pain offset +1% | +1% direct disease pain in that stage; no direct progression or growth effect. |
| Flesh stage boundaries | No direct impairment change with the current identical coefficients; labels/threat classification change. |
| Flesh nominal cap near 100 | No effect for ordinary Titan parts whose HP cap is lower. |

For a fresh 14.609-day major-stage growth window:

| Perturbation | Probability of any fully petrified part |
|---|---:|
| Baseline | 0.08283% |
| Growth frequency +1% | 0.08469% |
| Both growth-size bounds +1% | 0.08564% |
| Titan health scale +1%, with actual HP rounding | 0.07325% |

Infinitesimal risk elasticities are approximately 2.24 for growth frequency and 2.58 for size at this baseline. The finite 1% size response is larger than its linear approximation because additional hit combinations cross HP thresholds. Very rare minor-stage outcomes can have much larger relative elasticities while remaining negligible in absolute terms.

Additional nonlinearities:

- Raising the maximum major-stage growth above 11 opens single-hit full petrification for healthy Titan digits. A brain requires a single growth of at least 13. These are different risk regimes from repeat-hit accumulation.
- A small upward health-scale change can add a whole HP to several parts; at 1.3, a 1% increase raises brain HP from 13 to 14. A comparable downward change need not remove an HP.
- A medicine requirement of 2.02 rounds up to three units, while 1.98 still requires two. This is a resource-cost step, not a smooth derivative.
- Reducing surgery success from 98% to 97.02% adds about 1% to expected attempts but increases expected failed operations per successful removal by 50.5%. Success cannot improve beyond the 98% ceiling under ordinary surgery.
- Raising lethal severity without also raising its equal maximum can make systemic death unreachable. Initial, minimum, and lethal severity primarily change headroom and boundary proximity rather than rates.
- Increasing the 40% systemic threshold delays the transition to three times as many growths and 6.43 times the tissue production per active day. The magnitude depends on the pawn's severity trajectory; no universal per-percent workload derivative applies.

## Appendix E: implementation references and validation scope

Repository sources:

- [Disease, flesh, and excision definitions](XylXenos.Data/Defs/GeneDefs/GeneDefs_Petrification.xml)
- [Growth-mode transitions and per-episode progression roll](XylXenos/HediffComp_GrowthModeExt.cs)
- [Cumulative treatment quality and remission reset](XylXenos/HediffComp_PetrificationTendDuration.cs)
- [Flesh severity cap and efficiency scaling](XylXenos/Hediff_PetrifiedFlesh.cs)
- [Excision eligibility, wound creation, and automatic tending](XylXenos/Recipe_RemoveHediff_Petrified.cs)
- [Growth selection, stacking eligibility, and replacement exclusion](Xylib/HediffGiver_RandomExt.cs)
- [Dynamic hediff stages and tending permissions](Xylib/HediffWithCompsExt.cs)
- [Giant health scale](XylXenos.Data/Defs/GeneDefs/GeneDefs_Body.xml)
- [Titan gene set](XylXenos.Data/Defs/GeneDefs/XenotypeDefs_Titan.xml)
- [Softener prevention](XylXenos.Data/Defs/Drugs/Drugs_Titan.xml)
- [Disease incident](XylXenos.Data/Defs/Storyteller/Incidents.xml)

Installed-game behavior was inspected in the actual `Assembly-CSharp.dll`, rather than inferred from the reference assembly. Relevant methods include `HediffComp_TendDuration.CompTended`, `SeverityChangePerDay`, and `AllowTend`; `TendUtility.CalculateBaseTendQuality`; `JobDriver_TendPatient`; `BodyDef.CacheDataRecursive`; `BodyPartDef.GetMaxHealth`; `HediffSet.GetPartHealth`; artificial-part installation and recursive restoration; the surgery outcome pipeline; and ingredient-count rounding. The project currently compiles against `Krafs.Rimworld.Ref` 1.7.4977-alpha.1, while the inspected installed game is 1.7.4980 rev60.

Analytical cross-checks reproduce the industrial and glitterworld treatment counts and mean drift, and the fixed-window compound-Poisson risk model agrees with the first-loss simulations within sampling variation. The report does not provide a validated whole-pawn mortality simulation, exact surgical part-loss probabilities, a colony staffing simulation, or a full model of postoperative recovery. Its strongest results concern current rules, episode averages, medical work under stated policies, growth-only accumulation, and local tuning responses.
