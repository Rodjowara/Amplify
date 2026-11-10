# Amp

Needs Rust 1.88+, g++ and Qt 6.12+.

```
sudo apt install pkg-config libasound2-dev protobuf-compiler libgl-dev
pipx install aqtinstall
aqt install-qt linux desktop 6.12.0 linux_gcc_64 -O ~/Qt
curl -sSf https://sh.rustup.rs | sh
export QtDir=~/Qt/6.12.0/gcc_64
alias amp-run='PATH=$QtDir/bin:$PATH LD_LIBRARY_PATH=$QtDir/lib cargo run -p amp-app'
```

Run `amp-run` from `amp/`.
