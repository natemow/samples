# Queue module

This module requires `#jguiConfigData` `resources.queues` endpoints; each queue
should also have a corresponding `pages` config there using the same key. The
queue/page key is supplied to this module's `queue` directive, which fetches
data and outputs the table.

Paging is handled by the `jguiBootstrapUI` configuration; UI Bootstrap's
directive is present in `queue.html`.
