# Five city treks

Open **Treks** from the Dashboard to select a city. New York is the default. Sydney, London, Moscow and Paris are the other native dropdown choices. There are six named nodes on each illustrated trail; no unlabeled destination dots.

| City | Origin | 20 m street checkpoint | Later landmarks |
|---|---|---|---|
| New York | Times Square | Broadway | Bryant Park, Grand Central Terminal, Empire State Building, Central Park |
| Sydney | Circular Quay | Alfred Street | Sydney Opera House, The Rocks, Sydney Harbour Bridge, Sydney Town Hall |
| London | Trafalgar Square | Whitehall | Big Ben, London Eye, St James's Park, Buckingham Palace |
| Moscow | Red Square | Nikolskaya Street | St Basil's Cathedral, Bolshoi Theatre, Gorky Park, Sparrow Hills |
| Paris | Louvre | Rue de Rivoli | Tuileries Garden, Place de la Concorde, Arc de Triomphe, Eiffel Tower |

## What the distances mean

The 20 m street stop is an introductory virtual checkpoint near the origin, not a claim about a street's total length or an exact surveyed endpoint. It shares the origin's approximate geographic anchor. Its cumulative threshold is explicitly 0.020 km equivalent.

Each later stop adds a great-circle leg between approximate landmark anchors, using a spherical Earth radius of 6371.0088 km. These manually chosen coordinates are visible in `Adventure.cs`. They represent places/entrances approximately; they are not validated pedestrian routes. This construction adds the 20 m introduction to the landmark legs, so it is a playful route model, not geospatial navigation.

The illustrated winding curve uses equal visual space for every stop so the short street milestone stays visible. Its bends, compass orientation, spacing and travelled curve length do not describe the real geography. A purple traveller uses each leg's distance fraction mapped onto the sampled curve's length. All cumulative node labels after the street marker have an approximation sign; all values are screen equivalents.

Rough total trail equivalents: New York 4.0 km, Sydney 4.6 km, London 2.9 km, Moscow 9.1 km and Paris 5.2 km. The exact values come from the anchors and formula in the source.

Selecting a city maps the same lifetime distance onto that trail. It does not create a separate real-world distance ledger or reset the odometer. A complete trail starts another virtual lap. At an exact end threshold, the completed lap is shown; subsequent movement begins the next.

## Badge presentation

The ten global thresholds remain 100 m, 500 m, 1, 2, 5, 8, 10, 15, 50 and 100 km equivalents. Home shows a city explorer badge, street mini-stroll comparisons for the first two tiers, a landmark context for shorter city progress and an approximate number of city-trail completions for longer tiers. The badge progress bar runs from the previous global tier to the next.

History retains the original ten referenced comparisons: Olympic 100 m straight; Sydney Harbour Bridge's 503 m span; two spans; Golden Gate Bridge's 2.737 km total including approaches; half a Central Park approximately 6.1-mile loop; three Golden Gate lengths; just over the Central Park loop; Sydney's 14 km City2Surf; beyond a standard 42.195 km marathon; beyond a 96 km Kokoda Track comparison. These are comparisons to round thresholds, not claims that the reference has the threshold's exact length.

## Landmark/reference links

These describe landmarks and reference lengths. They are not sources of surveyed game-route distances or street directions.

- New York: [Times Square Alliance](https://www.timessquarenyc.org/), [Bryant Park](https://bryantpark.org/), [Grand Central Terminal](https://grandcentralterminal.com/).
- Sydney: [Circular Quay, official Sydney tourism](https://www.sydney.com/destinations/sydney/sydney-city/circular-quay), [Opera House visitor directions](https://www.sydneyoperahouse.com/visit/getting-here).
- London: [Westminster, official Visit London guide](https://www.visitlondon.com/things-to-do/london-areas/westminster).
- Moscow: [Bolshoi Theatre](https://bolshoi.ru/en/), [Gorky Park](https://park-gorkogo.ru/).
- Paris: [Paris tourism](https://parisjetaime.com/eng/), [Louvre visitor directions](https://www.louvre.fr/en/visit/map-entrances-directions).
- Badge references: [World Athletics 100 m](https://worldathletics.org/disciplines/sprints/100-metres), [Golden Gate Bridge statistics](https://www.goldengate.org/bridge/history-research/statistics-data/design-construction-stats/), [Central Park running](https://www.centralparknyc.org/activities/guides/running-guide), [City2Surf](https://city2surf.com.au/), [Kokoda Track, Australian DVA](https://anzacportal.dva.gov.au/wars-and-missions/world-war-ii-1939-1945/events/kokoda-track).

No online map, location permission, GPS or route service is used by the app.
