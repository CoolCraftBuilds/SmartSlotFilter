# Changelog

## 1.1.0

Filter layouts can now be copied across a whole container instead of one slot at a time.

- **Copy all filters** / **Paste all filters** — copies every filter in a container and
  applies them to another container of the same kind. Filters are duplicated with the
  game's own clone, so the **allowed quality** settings travel with them; that is the part
  that is tedious to redo by hand, and the reason per-slot copying falls short when eight
  stations run the same recipe.
- **Clear all filters** — clears a whole container in one click. Previously the only way to
  do this was to copy an unconfigured station over it.
- A paste is **refused** rather than partly applied when the target is a different kind of
  container or has a different number of slots, and the reason appears on the button you
  clicked. Half a layout on a station you believed was identical does not show up until
  something is already wrong in the mix.
- Pasting a layout that contains no filters is refused too, now that clearing has its own
  button.
- Every entry this mod adds says "all", so one glance separates them from the game's own
  per-slot Copy, Paste and Clear in the same menu.

Confirmed working on 0.4.6f13.

## 1.0.0

First release.

- **From current item** — sets a slot's filter to a whitelist of the item already in it.
- **Filter all from items** — does the same for every non-empty slot in the container.
