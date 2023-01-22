mamba search fastai --info -c fastchan

======

conda install pytorch torchvision torchaudio cpuonly -c pytorch

pytorch-cpu 1.13.0 cpu_py310h587aeda_0
--------------------------------------
file name   : pytorch-cpu-1.13.0-cpu_py310h587aeda_0.conda
name        : pytorch-cpu
version     : 1.13.0
build       : cpu_py310h587aeda_0
build number: 0
size        : 17 KB
license     : BSD-3-Clause
subdir      : linux-64
url         : https://conda.anaconda.org/conda-forge/linux-64/pytorch-cpu-1.13.0-cpu_py310h587aeda_0.conda
md5         : 4e8e51b1c58ae096b2b566f5d34d30f5
timestamp   : 2022-12-03 16:55:16 UTC
track_features: 
  - pytorch-cpu
dependencies: 
  - pytorch 1.13.0 cpu_py310h02c325b_0

pytorch 1.13.0 cpu_py310h02c325b_0
----------------------------------
file name   : pytorch-1.13.0-cpu_py310h02c325b_0.conda
name        : pytorch
version     : 1.13.0
build       : cpu_py310h02c325b_0
build number: 0
size        : 62.6 MB
license     : BSD-3-Clause
subdir      : linux-64
url         : https://conda.anaconda.org/conda-forge/linux-64/pytorch-1.13.0-cpu_py310h02c325b_0.conda
md5         : 9c496cba4035c617cba62a6522bdd7b2
timestamp   : 2022-12-03 16:52:11 UTC
constraints : 
  - pytorch-cpu = 1.13.0
  - pytorch-gpu = 99999999
dependencies: 
  - __glibc >=2.17,<3.0.a0
  - _openmp_mutex >=4.5
  - cffi
  - libcblas >=3.9.0,<4.0a0
  - libgcc-ng >=12
  - libprotobuf >=3.21.10,<3.22.0a0
  - libstdcxx-ng >=12
  - mkl >=2022.2.1,<2023.0a0
  - ninja
  - numpy >=1.21.6,<2.0a0
  - python >=3.10,<3.11.0a0
  - python_abi 3.10.* *_cp310
  - setuptools
  - sleef >=3.5.1,<4.0a0
  - typing_extensions

  torchvision 0.14.1 py310_cpu
----------------------------
file name   : torchvision-0.14.1-py310_cpu.tar.bz2
name        : torchvision
version     : 0.14.1
build       : py310_cpu
build number: 0
size        : 6.4 MB
license     : BSD
subdir      : linux-64
url         : https://conda.anaconda.org/pytorch/linux-64/torchvision-0.14.1-py310_cpu.tar.bz2
md5         : 68696db9887f1b5b7f1021ec5959331f
timestamp   : 2022-12-09 02:53:18 UTC
constraints : 
  - cpuonly
dependencies: 
  - ffmpeg >=4.2
  - jpeg
  - libpng
  - numpy >=1.11
  - pillow >=5.3.0,!=8.3.*
  - python >=3.10,<3.11.0a0
  - pytorch 1.13.1
  - pytorch-mutex 1.0 cpu
  - requests

  torchaudio 0.13.1 py310_cpu
---------------------------
file name   : torchaudio-0.13.1-py310_cpu.tar.bz2
name        : torchaudio
version     : 0.13.1
build       : py310_cpu
build number: 0
size        : 6.4 MB
license     : BSD
subdir      : linux-64
url         : https://conda.anaconda.org/pytorch/linux-64/torchaudio-0.13.1-py310_cpu.tar.bz2
md5         : 1b08249d81c504aad2d581fabab23dc1
timestamp   : 2022-12-10 02:15:44 UTC
constraints : 
  - cpuonly
dependencies: 
  - numpy >=1.21.2
  - python >=3.10,<3.11.0a0
  - pytorch 1.13.1
  - pytorch-mutex 1.0 cpu

  cpuonly 2.0 0
-------------
file name   : cpuonly-2.0-0.tar.bz2
name        : cpuonly
version     : 2.0
build       : 0
build number: 0
size        : 2 KB
subdir      : noarch
url         : https://conda.anaconda.org/pytorch/noarch/cpuonly-2.0-0.tar.bz2
md5         : 1cf3a59ef90a4078c253e3b02c272065
timestamp   : 2021-09-06 07:33:35 UTC
track_features: 
  - cpuonly
dependencies: 
  - pytorch-mutex 1.0 cpu

======

conda install pytorch torchvision torchaudio pytorch-cuda=11.7 -c pytorch -c nvidia

pytorch 1.13.0 cuda112py310he33e0d6_200
---------------------------------------
file name   : pytorch-1.13.0-cuda112py310he33e0d6_200.conda
name        : pytorch
version     : 1.13.0
build       : cuda112py310he33e0d6_200
build number: 200
size        : 341.0 MB
license     : BSD-3-Clause
subdir      : linux-64
url         : https://conda.anaconda.org/conda-forge/linux-64/pytorch-1.13.0-cuda112py310he33e0d6_200.conda
md5         : 3e1b362611902786b1fc07e2e2323bba
timestamp   : 2022-12-03 03:12:53 UTC
constraints : 
  - pytorch-cpu = 99999999
  - pytorch-gpu = 1.13.0
dependencies: 
  - __cuda
  - __glibc >=2.17
  - __glibc >=2.17,<3.0.a0
  - _openmp_mutex >=4.5
  - cffi
  - cudatoolkit >=11.2,<12
  - cudnn >=8.4.1.50,<9.0a0
  - libcblas >=3.9.0,<4.0a0
  - libgcc-ng >=12
  - libprotobuf >=3.21.10,<3.22.0a0
  - libstdcxx-ng >=12
  - magma >=2.6.2,<2.6.3.0a0
  - mkl >=2022.2.1,<2023.0a0
  - nccl >=2.14.3.1,<3.0a0
  - ninja
  - numpy >=1.21.6,<2.0a0
  - python >=3.10,<3.11.0a0
  - python_abi 3.10.* *_cp310
  - setuptools
  - sleef >=3.5.1,<4.0a0
  - typing_extensions

  torchvision 0.14.1 py310_cu117
------------------------------
file name   : torchvision-0.14.1-py310_cu117.tar.bz2
name        : torchvision
version     : 0.14.1
build       : py310_cu117
build number: 0
size        : 7.8 MB
license     : BSD
subdir      : linux-64
url         : https://conda.anaconda.org/pytorch/linux-64/torchvision-0.14.1-py310_cu117.tar.bz2
md5         : d2aead3dc1b8facab1c62a61f2ec8f1e
timestamp   : 2022-12-09 03:04:09 UTC
constraints : 
  - cpuonly <0
dependencies: 
  - ffmpeg >=4.2
  - jpeg
  - libpng
  - numpy >=1.11
  - pillow >=5.3.0,!=8.3.*
  - python >=3.10,<3.11.0a0
  - pytorch 1.13.1
  - pytorch-cuda 11.7.*
  - pytorch-mutex 1.0 cuda
  - requests

  torchaudio 0.13.1 py310_cu117
-----------------------------
file name   : torchaudio-0.13.1-py310_cu117.tar.bz2
name        : torchaudio
version     : 0.13.1
build       : py310_cu117
build number: 0
size        : 6.5 MB
license     : BSD
subdir      : linux-64
url         : https://conda.anaconda.org/pytorch/linux-64/torchaudio-0.13.1-py310_cu117.tar.bz2
md5         : 67180595317ad698f48d4d10942eaa36
timestamp   : 2022-12-10 02:26:21 UTC
constraints : 
  - cpuonly <0
dependencies: 
  - numpy >=1.21.2
  - python >=3.10,<3.11.0a0
  - pytorch 1.13.1
  - pytorch-cuda 11.7.*
  - pytorch-mutex 1.0 cuda

  pytorch-cuda 11.7 h67b0de4_1
----------------------------
file name   : pytorch-cuda-11.7-h67b0de4_1.tar.bz2
name        : pytorch-cuda
version     : 11.7
build       : h67b0de4_1
build number: 1
size        : 3 KB
subdir      : noarch
url         : https://conda.anaconda.org/pytorch/noarch/pytorch-cuda-11.7-h67b0de4_1.tar.bz2
md5         : 861b9bb6e2b3642de594c0aebf44c64e
timestamp   : 2022-12-13 00:53:26 UTC
constraints : 
  - cuda-tools >=11.7,<11.8
  - cuda-command-line-tools >=11.7,<11.8
  - libnvjpeg-dev >=11.7.2.34,<11.9.0.86
  - libnpp >=11.7.3.21,<11.8.0.86
  - cuda-nvrtc-dev >=11.7,<11.8
  - cuda-cudaart-dev >=11.7,<11.8
  - cuda-nvtx >=11.7,<11.8
  - cuda-driver-dev >=11.7,<11.8
  - libcusparse >=11.7.3.50,<11.7.5.86
  - libcusparse-dev >=11.7.3.50,<11.7.5.86
  - cuda-nvml-dev >=11.7,<11.8
  - cuda-cccl >=11.7,<11.8
  - cuda-runtime >=11.7,<11.8
  - libcufft-dev >=10.7.2.50,<10.9.0.58
  - cuda-compiler >=11.7,<11.8
  - libcublas-dev >=11.10.1.25,<11.11.3.6
  - libcusolver >=11.3.5.50,<11.4.1.48
  - cuda-nvcc >=11.7,<11.8
  - libcusolver-dev >=11.3.5.50,<11.4.1.48
  - libnpp-dev >=11.7.3.21,<11.8.0.86
  - libnvjpeg >=11.7.2.34,<11.9.0.86
  - cuda-nvrtc >=11.7,<11.8
  - cuda-libraries >=11.7,<11.8
  - cuda-cuobjdump >=11.7,<11.8
  - libcufft >=10.7.2.50,<10.9.0.58
  - libcublas >=11.10.1.25,<11.11.3.6
  - cuda-cudart >=11.7,<11.8
  - cuda-cudart-dev >=11.7,<11.8
  - cuda-toolkit >=11.7,<11.8
  - cuda-cupti >=11.7,<11.8
  - cuda-libraries-dev >=11.7,<11.8
  - cuda-nvprune >=11.7,<11.8
  - cuda-cuxxfilt >=11.7,<11.8
dependencies: 
  - cuda 11.7.*

  ======

  tensorflow-gpu 2.10.0 cuda112py310h0bbbad9_0
--------------------------------------------
file name   : tensorflow-gpu-2.10.0-cuda112py310h0bbbad9_0.tar.bz2
name        : tensorflow-gpu
version     : 2.10.0
build       : cuda112py310h0bbbad9_0
build number: 0
size        : 28 KB
license     : Apache-2.0
subdir      : linux-64
url         : https://conda.anaconda.org/conda-forge/linux-64/tensorflow-gpu-2.10.0-cuda112py310h0bbbad9_0.tar.bz2
md5         : b602809e05b698ada2bbf00b25a1308d
timestamp   : 2022-09-15 02:49:35 UTC
dependencies: 
  - tensorflow 2.10.0 cuda112py310he87a039_0

 tensorflow 2.10.0 cuda112py310he87a039_0
----------------------------------------
file name   : tensorflow-2.10.0-cuda112py310he87a039_0.tar.bz2
name        : tensorflow
version     : 2.10.0
build       : cuda112py310he87a039_0
build number: 0
size        : 29 KB
license     : Apache-2.0
subdir      : linux-64
url         : https://conda.anaconda.org/conda-forge/linux-64/tensorflow-2.10.0-cuda112py310he87a039_0.tar.bz2
md5         : c667ef0245f219cecd96ec508bfbf0bf
timestamp   : 2022-09-15 02:49:31 UTC
dependencies: 
  - __cuda
  - python >=3.10,<3.11.0a0
  - python_abi 3.10.* *_cp310
  - tensorflow-base 2.10.0 cuda112py310hf679b68_0
  - tensorflow-estimator 2.10.0 cuda112py310h2fa73eb_0

tensorflow-base 2.10.0 cuda112py310hf679b68_0
---------------------------------------------
file name   : tensorflow-base-2.10.0-cuda112py310hf679b68_0.tar.bz2
name        : tensorflow-base
version     : 2.10.0
build       : cuda112py310hf679b68_0
build number: 0
size        : 400.1 MB
license     : Apache-2.0
subdir      : linux-64
url         : https://conda.anaconda.org/conda-forge/linux-64/tensorflow-base-2.10.0-cuda112py310hf679b68_0.tar.bz2
md5         : fdedc75098ed86dc5a5b84e3c15044c2
timestamp   : 2022-09-15 02:44:53 UTC
dependencies: 
  - __cuda
  - __glibc >=2.17
  - absl-py >=1.0.0
  - astunparse >=1.6.0
  - cudatoolkit >=11.2,<12
  - cudnn >=8.4.1.50,<9.0a0
  - flatbuffers >=2,<3.0.0.0a0
  - gast >=0.2.1,<=0.4.0
  - giflib >=5.2.1,<5.3.0a0
  - google-pasta >=0.1.1
  - grpc-cpp >=1.47.1,<1.48.0a0
  - grpcio 1.47.*
  - h5py >=2.9.0
  - icu >=70.1,<71.0a0
  - jpeg >=9e,<10a
  - keras >=2.10,<2.11
  - keras-preprocessing >=1.1.1
  - libabseil 20220623.0 cxx17*
  - libcurl >=7.83.1,<8.0a0
  - libgcc-ng >=12
  - libpng >=1.6.37,<1.7.0a0
  - libprotobuf >=3.21.5,<3.22.0a0
  - libsqlite >=3.39.3,<4.0a0
  - libstdcxx-ng >=12
  - libzlib >=1.2.12,<1.3.0a0
  - nccl >=2.14.3.1,<3.0a0
  - numpy >=1.21.6,<2.0a0
  - openssl >=1.1.1q,<1.1.2a
  - opt_einsum >=2.3.2
  - packaging
  - protobuf >=3.9.2
  - python >=3.10,<3.11.0a0
  - python-flatbuffers >=2
  - python_abi 3.10.* *_cp310
  - six >=1.12
  - snappy >=1.1.9,<2.0a0
  - tensorboard >=2.10,<2.11
  - termcolor >=1.1.0
  - typing_extensions >=3.6.6
  - wrapt >=1.11.0

tensorflow-estimator 2.10.0 cuda112py310h2fa73eb_0
--------------------------------------------------
file name   : tensorflow-estimator-2.10.0-cuda112py310h2fa73eb_0.tar.bz2
name        : tensorflow-estimator
version     : 2.10.0
build       : cuda112py310h2fa73eb_0
build number: 0
size        : 623 KB
license     : Apache-2.0
subdir      : linux-64
url         : https://conda.anaconda.org/conda-forge/linux-64/tensorflow-estimator-2.10.0-cuda112py310h2fa73eb_0.tar.bz2
md5         : 642ef6b16ef417d6aa1a2c31282c0576
timestamp   : 2022-09-15 02:49:21 UTC
dependencies: 
  - __glibc >=2.17
  - cudatoolkit >=11.2,<12
  - libgcc-ng >=12
  - libstdcxx-ng >=12
  - openssl >=1.1.1q,<1.1.2a
  - python >=3.10,<3.11.0a0
  - python_abi 3.10.* *_cp310
  - tensorflow-base 2.10.0 cuda112py310hf679b68_0

======

tensorflow-cpu 2.10.0 cpu_py310h718b53a_0
-----------------------------------------
file name   : tensorflow-cpu-2.10.0-cpu_py310h718b53a_0.tar.bz2
name        : tensorflow-cpu
version     : 2.10.0
build       : cpu_py310h718b53a_0
build number: 0
size        : 28 KB
license     : Apache-2.0
subdir      : linux-64
url         : https://conda.anaconda.org/conda-forge/linux-64/tensorflow-cpu-2.10.0-cpu_py310h718b53a_0.tar.bz2
md5         : 738e59b0d76e25092d7090b9c0106ad3
timestamp   : 2022-09-23 18:26:47 UTC
dependencies: 
  - tensorflow 2.10.0 cpu_py310hd1aba9c_0

tensorflow 2.10.0 cpu_py310hd1aba9c_0
-------------------------------------
file name   : tensorflow-2.10.0-cpu_py310hd1aba9c_0.tar.bz2
name        : tensorflow
version     : 2.10.0
build       : cpu_py310hd1aba9c_0
build number: 0
size        : 29 KB
license     : Apache-2.0
subdir      : linux-64
url         : https://conda.anaconda.org/conda-forge/linux-64/tensorflow-2.10.0-cpu_py310hd1aba9c_0.tar.bz2
md5         : aa4fc26a778ea22a7d3369ed98449cd0
timestamp   : 2022-09-23 18:26:43 UTC
track_features: 
  - tensorflow-cpu
dependencies: 
  - python >=3.10,<3.11.0a0
  - python_abi 3.10.* *_cp310
  - tensorflow-base 2.10.0 cpu_py310hc537a0e_0 => 204.2 MB
  - tensorflow-estimator 2.10.0 cpu_py310had6d012_0 => 

  ------------------------------------------
file name   : tensorflow-base-2.10.0-cpu_py310hc537a0e_0.tar.bz2
name        : tensorflow-base
version     : 2.10.0
build       : cpu_py310hc537a0e_0
build number: 0
size        : 153.9 MB
license     : Apache-2.0
subdir      : linux-64
url         : https://conda.anaconda.org/conda-forge/linux-64/tensorflow-base-2.10.0-cpu_py310hc537a0e_0.tar.bz2
md5         : 31ff1276b1850eb2566120f696079e1e
timestamp   : 2022-09-23 18:23:53 UTC
track_features: 
  - tensorflow-cpu
dependencies: 
  - absl-py >=1.0.0
  - astunparse >=1.6.0
  - flatbuffers >=2,<3.0.0.0a0
  - gast >=0.2.1,<=0.4.0
  - giflib >=5.2.1,<5.3.0a0
  - google-pasta >=0.1.1
  - grpc-cpp >=1.47.1,<1.48.0a0
  - grpcio 1.47.*
  - h5py >=2.9.0
  - icu >=70.1,<71.0a0
  - jpeg >=9e,<10a
  - keras >=2.10,<2.11
  - keras-preprocessing >=1.1.1
  - libabseil 20220623.0 cxx17*
  - libcurl >=7.83.1,<8.0a0
  - libgcc-ng >=12
  - libpng >=1.6.38,<1.7.0a0
  - libprotobuf >=3.21.6,<3.22.0a0
  - libsqlite >=3.39.3,<4.0a0
  - libstdcxx-ng >=12
  - libzlib >=1.2.12,<1.3.0a0
  - numpy >=1.21.6,<2.0a0
  - openssl >=1.1.1q,<1.1.2a
  - opt_einsum >=2.3.2
  - packaging
  - protobuf >=3.9.2
  - python >=3.10,<3.11.0a0
  - python-flatbuffers >=2
  - python_abi 3.10.* *_cp310
  - six >=1.12
  - snappy >=1.1.9,<2.0a0
  - tensorboard >=2.10,<2.11
  - termcolor >=1.1.0
  - typing_extensions >=3.6.6
  - wrapt >=1.11.0

tensorflow-estimator 2.10.0 cpu_py310had6d012_0
-----------------------------------------------
file name   : tensorflow-estimator-2.10.0-cpu_py310had6d012_0.tar.bz2
name        : tensorflow-estimator
version     : 2.10.0
build       : cpu_py310had6d012_0
build number: 0
size        : 627 KB
license     : Apache-2.0
subdir      : linux-64
url         : https://conda.anaconda.org/conda-forge/linux-64/tensorflow-estimator-2.10.0-cpu_py310had6d012_0.tar.bz2
md5         : 8fc6e2b511b72000744d0bc56b66e32c
timestamp   : 2022-09-23 18:26:34 UTC
dependencies: 
  - libgcc-ng >=12
  - libstdcxx-ng >=12
  - openssl >=1.1.1q,<1.1.2a
  - python >=3.10,<3.11.0a0
  - python_abi 3.10.* *_cp310
  - tensorflow-base 2.10.0 cpu_py310hc537a0e_0

  ======

fastai 2.7.10 py_0
------------------
file name   : fastai-2.7.10-py_0.tar.bz2
name        : fastai
version     : 2.7.10
build       : py_0
build number: 0
size        : 184 KB
license     : Apache Software
subdir      : noarch
url         : https://conda.anaconda.org/fastchan/noarch/fastai-2.7.10-py_0.tar.bz2
md5         : faa82511571e621eafb0175ca036abe1
timestamp   : 2022-11-02 03:07:50 UTC
dependencies: 
  - fastcore >=1.4.5,<1.6
  - fastdownload >=0.0.5,<2
  - fastprogress >=0.2.4
  - matplotlib
  - packaging
  - pandas
  - pillow >6.0.0
  - pip
  - python
  - pytorch >=1.7,<1.14
  - pyyaml
  - requests
  - scikit-learn
  - scipy
  - spacy <4
  - torchvision >=0.8.2

timm 0.6.12 pyhd8ed1ab_0
------------------------
file name   : timm-0.6.12-pyhd8ed1ab_0.conda
name        : timm
version     : 0.6.12
build       : pyhd8ed1ab_0
build number: 0
size        : 324 KB
license     : Apache-2.0
subdir      : noarch
url         : https://conda.anaconda.org/conda-forge/noarch/timm-0.6.12-pyhd8ed1ab_0.conda
md5         : 35b38f2a15daa401c6242e8cd795dedd
timestamp   : 2022-11-24 16:31:34 UTC
dependencies: 
  - huggingface_hub
  - python >=3.6
  - pytorch >=1.7
  - pyyaml
  - torchvision

  ======

transformers 4.24.0 pyhd8ed1ab_0
--------------------------------
file name   : transformers-4.24.0-pyhd8ed1ab_0.tar.bz2
name        : transformers
version     : 4.24.0
build       : pyhd8ed1ab_0
build number: 0
size        : 2.7 MB
license     : Apache-2.0
subdir      : noarch
url         : https://conda.anaconda.org/conda-forge/noarch/transformers-4.24.0-pyhd8ed1ab_0.tar.bz2
md5         : 875ee29e2e777897f0e5f94bbecad24d
timestamp   : 2022-11-03 07:54:45 UTC
dependencies: 
  - dataclasses
  - datasets
  - filelock
  - huggingface_hub
  - importlib_metadata
  - numpy
  - packaging
  - python >=3.7
  - pytorch
  - pyyaml
  - regex !=2019.12.17
  - requests
  - sacremoses
  - tokenizers >=0.11.1,!=0.11.3
  - tqdm >=4.27

  =====

spacy 3.4.3 py310hc4a4660_1
---------------------------
file name   : spacy-3.4.3-py310hc4a4660_1.tar.bz2
name        : spacy
version     : 3.4.3
build       : py310hc4a4660_1
build number: 1
size        : 6.4 MB
license     : MIT
subdir      : linux-64
url         : https://conda.anaconda.org/conda-forge/linux-64/spacy-3.4.3-py310hc4a4660_1.tar.bz2
md5         : 228859361f0a966d5a8bb5b606b2469e
timestamp   : 2022-11-16 16:19:54 UTC
dependencies: 
  - catalogue >=2.0.6,<2.1.0
  - cymem >=2.0.2,<2.1.0
  - jinja2
  - langcodes >=3.2.0,<4.0.0
  - libgcc-ng >=12
  - libstdcxx-ng >=12
  - murmurhash >=0.28.0,<1.1.0
  - numpy >=1.21.6,<2.0a0
  - packaging >=20.0
  - pathy >=0.3.5
  - preshed >=3.0.2,<3.1.0
  - pydantic >=1.7.4,!=1.8,!=1.8.1,<1.11.0
  - python >=3.10,<3.11.0a0
  - python_abi 3.10.* *_cp310
  - requests >=2.13.0,<3.0.0
  - setuptools
  - spacy-legacy >=3.0.10,<3.1.0
  - spacy-loggers >=1.0.0,<2.0.0
  - srsly >=2.4.3,<3.0.0
  - thinc >=8.1.0,<8.2.0
  - tqdm >=4.38.0,<5.0.0
  - typer >=0.3.2,<0.5.0
  - wasabi >=0.9.1,<1.1.0

cupy 11.4.0 py310h9216885_0
---------------------------
file name   : cupy-11.4.0-py310h9216885_0.conda
name        : cupy
version     : 11.4.0
build       : py310h9216885_0
build number: 0
size        : 34.5 MB
license     : MIT
subdir      : linux-64
url         : https://conda.anaconda.org/conda-forge/linux-64/cupy-11.4.0-py310h9216885_0.conda
md5         : 232426dcf3e75f212c084a71092a89d8
timestamp   : 2022-12-08 15:35:19 UTC
constraints : 
  - __glibc >=2.17
  - cudnn >=8.4.1.50,<9.0a0
  - cutensor >=1.3,<2.0a0
  - cusparselt >=0.2.0.1,<0.3.0a0
  - optuna >=2
  - scipy >=1.4
  - nccl >=2.14.3.1,<3.0a0
dependencies: 
  - __glibc >=2.17
  - __glibc >=2.17,<3.0.a0
  - cudatoolkit >=11.2,<12
  - fastrlock >=0.8,<0.9.0a0
  - libgcc-ng >=12
  - libstdcxx-ng >=12
  - numpy >=1.18
  - python >=3.10,<3.11.0a0

  ======

  nbdev 2.3.9 py_0
----------------
file name   : nbdev-2.3.9-py_0.tar.bz2
name        : nbdev
version     : 2.3.9
build       : py_0
build number: 0
size        : 61 KB
license     : Apache Software
subdir      : noarch
url         : https://conda.anaconda.org/fastai/noarch/nbdev-2.3.9-py_0.tar.bz2
md5         : 87ce241d95b5107d87fc9b0a4e5a7abe
timestamp   : 2022-11-08 23:19:55 UTC
dependencies: 
  - asttokens
  - astunparse
  - execnb >=0.1.4
  - fastcore >=1.5.27
  - ghapi >=1.0.3
  - packaging
  - pip
  - python
  - pyyaml
  - watchdog