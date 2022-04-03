#!/bin/bash
if not:qemu-system-x86_64 --version

apt-get update -y
apt-get install -y cpu-checker

if:kvm-ok

apt-get install -y qemu qemu-utils qemu-kvm 

if not:~/.ssh/id_rsa

ssh-keygen

# wget https://cloud-images.ubuntu.com/minimal/releases/focal/release-20220201/ubuntu-20.04-minimal-cloudimg-amd64.img