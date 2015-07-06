# Entry module

The `entry` module is responsible for how an entry is presented in particular
queues, and for posting resulting actions back to the server.

The `entry` directive lives in the `queue` module's HTML. Modal, scoring and tab
support are provided to the directive via `config` module's `jguiBootstrapUi`
service.
