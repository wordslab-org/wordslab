> k3s ctr images list

docker.io/library/ubuntu:22.04
sha256:27cb6e6ccef575a4698b66f5de06c7ecd61589132d5a91d098f7f3f9285415a9 
29.0 MiB

> k3s ctr containers list

docker.io/library/ubuntu:22.04
7365b217354df98af03dc9bfef6a0188a667f4690c78c0589ef4c0458b95bb2b 

> k3s ctr containers info 7365b217354df98af03dc9bfef6a0188a667f4690c78c0589ef4c0458b95bb2b  | grep SnapshotKey

"SnapshotKey": "7365b217354df98af03dc9bfef6a0188a667f4690c78c0589ef4c0458b95bb2b",

> k3s ctr snapshots info 7365b217354df98af03dc9bfef6a0188a667f4690c78c0589ef4c0458b95bb2b

{
    "Kind": "Active",
    "Name": "7365b217354df98af03dc9bfef6a0188a667f4690c78c0589ef4c0458b95bb2b",
    "Parent": "sha256:6515074984c6f8bb1b8a9962c8fb5f310fc85e70b04c88442a3939c026dbfad3",
    "Created": "2022-12-31T10:55:17.267285Z",
    "Updated": "2022-12-31T10:55:17.267285Z"
}

> k3s ctr snapshots usage | grep 7365b217354df98af03dc9bfef6a0188a667f4690c78c0589ef4c0458b95bb2b

KEY                                                                     SIZE      INODES
7365b217354df98af03dc9bfef6a0188a667f4690c78c0589ef4c0458b95bb2b        100.0 KiB 30

> k3s ctr snapshots info sha256:6515074984c6f8bb1b8a9962c8fb5f310fc85e70b04c88442a3939c026dbfad3

{
    "Kind": "Committed",
    "Name": "sha256:6515074984c6f8bb1b8a9962c8fb5f310fc85e70b04c88442a3939c026dbfad3",
    "Labels": {
        "containerd.io/snapshot.ref": "sha256:6515074984c6f8bb1b8a9962c8fb5f310fc85e70b04c88442a3939c026dbfad3"
    },
    "Created": "2022-12-16T23:02:11.6271932Z",
    "Updated": "2022-12-16T23:02:11.6271932Z"
}

> k3 ctr snapshots usage | grep sha256:6515074984c6f8bb1b8a9962c8fb5f310fc85e70b04c88442a3939c026dbfad3

KEY                                                                     SIZE      INODES
sha256:6515074984c6f8bb1b8a9962c8fb5f310fc85e70b04c88442a3939c026dbfad3 83.4 MiB  3515