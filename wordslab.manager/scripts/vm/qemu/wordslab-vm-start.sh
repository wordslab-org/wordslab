#!/bin/bash
qemu-system-x86_64 \
  -machine accel=kvm,type=q35 \
  -cpu host \
  -smp $(nproc) \
  -m 8G \
  -nographic \
  -device virtio-net-pci,netdev=net0 \
  -netdev user,id=net0,hostfwd=tcp::3022-:22,hostfwd=tcp::3080-:80,hostfwd=tcp::3443-:6443 \
  -drive if=virtio,format=qcow2,file=wordslab-os.img \
  -drive if=virtio,format=raw,file=seed.img \
  -drive if=virtio,format=qcow2,file=wordslab-cluster.img \
  -drive if=virtio,format=qcow2,file=wordslab-data.img