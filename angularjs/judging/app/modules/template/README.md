# Template module

This module controls output for the main `index.volt` file, handles routing and
provides a lightweight page-level templating mechanism to control the main
content area (`ng-view` in `index.volt`).

Pages are defined in the inline `#jguiConfigData` script in `index.volt`. To use
a custom template instead of `default.html`, just set the
`template: 'whatever.html'` var in your page config and add your new template
to this directory.

Routing is set up to use a single dynamic path segment; the key defined per
page config becomes that path segment.
