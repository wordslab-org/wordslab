#!/bin/bash
cloud-localds seed.img user-data.yaml metadata.yaml
qemu-img create -b ubuntu-20.04-minimal-cloudimg-amd64.img -f qcow2 wordslab-os.img
qemu-img create -f qcow2 wordslab-cluster.img 10G
qemu-img create -f qcow2 wordslab-data.img 10G