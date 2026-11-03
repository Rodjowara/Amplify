# Amp

## Linux setup

Needs Rust 1.88+, g++ and **Qt 6.12+**.

```
aqt install-qt linux desktop 6.12.0 linux_gcc_64 -O ~/Qt
curl -sSf https://sh.rustup.rs | sh
# add to your shell profile:
export QtDir=~/Qt/6.12.0/gcc_64
alias amp-run='PATH=$QtDir/bin:$PATH LD_LIBRARY_PATH=$QtDir/lib cargo run -p amp-app'
```

Then run `amp-run` from `amp/`. Qt Bridge for Rust comes from crates.io, so there's nothing to install for it.
