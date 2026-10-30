param(
    [string]$WorkDir = (Join-Path $PSScriptRoot 'work'),
    [string]$LlvmMingwRoot,
    [string]$Cmake,
    [string]$Ninja
)

$ErrorActionPreference = 'Stop'

$OpencvCommit = 'fe38fc608f6acb8b68953438a62305d8318f4fcd'
$OpencvSharpCommit = 'b161e7e012f5101f6d5dc68a835c59db6cc88b18'

if (-not $LlvmMingwRoot) { $LlvmMingwRoot = Join-Path $WorkDir 'tools/llvm-mingw-20260616-ucrt-x86_64' }
if (-not $Cmake) { $Cmake = Join-Path $WorkDir 'tools/cmake-3.31.8-windows-x86_64/bin/cmake.exe' }
if (-not $Ninja) { $Ninja = Join-Path $WorkDir 'tools/ninja/ninja.exe' }

$source = Join-Path $WorkDir "src/opencv-$OpencvCommit"
$wrapper = Join-Path $WorkDir "src/opencvsharp-$OpencvSharpCommit/src"
$install = Join-Path $WorkDir 'install-simd'
$buildOpenCv = Join-Path $WorkDir 'build-opencv-simd'
$buildWrapper = Join-Path $WorkDir 'build-wrapper-simd'
$compilerRoot = Join-Path $LlvmMingwRoot 'bin'

foreach ($path in @($source, $wrapper, $Cmake, $Ninja, (Join-Path $compilerRoot 'clang.exe'))) {
    if (-not (Test-Path -LiteralPath $path)) { throw "缺少构建依赖：$path（准备步骤见 native/README.md）" }
}

$env:LLVM_MINGW_ROOT = $LlvmMingwRoot
$env:PATH = $compilerRoot + ';' + $env:PATH

$toolchain = Join-Path $PSScriptRoot 'toolchain-llvm-mingw.cmake'
$common = @(
    '-G', 'Ninja',
    "-DCMAKE_MAKE_PROGRAM=$Ninja",
    "-DCMAKE_C_COMPILER=$compilerRoot/clang.exe",
    "-DCMAKE_CXX_COMPILER=$compilerRoot/clang++.exe",
    '-DCMAKE_BUILD_TYPE=Release',
    "-DCMAKE_TOOLCHAIN_FILE=$toolchain",
    '-DCMAKE_CXX_FLAGS=-DNOMINMAX',
    '-DCMAKE_EXE_LINKER_FLAGS=-static',
    '-DCMAKE_SHARED_LINKER_FLAGS=-static -Wl,--gc-sections'
)

# 只编译 core/imgproc/imgcodecs，并关闭无关的编解码器与后端。
& $Cmake -S $source -B $buildOpenCv @common "-DCMAKE_INSTALL_PREFIX=$install" `
    '-DBUILD_LIST=core,imgproc,imgcodecs' '-DBUILD_SHARED_LIBS=OFF' `
    '-DBUILD_TESTS=OFF' '-DBUILD_PERF_TESTS=OFF' '-DBUILD_EXAMPLES=OFF' `
    '-DBUILD_opencv_apps=OFF' '-DBUILD_opencv_world=OFF' '-DBUILD_JAVA=OFF' `
    '-DBUILD_opencv_python2=OFF' '-DBUILD_opencv_python3=OFF' `
    '-DWITH_IPP=OFF' '-DWITH_OPENCL=OFF' '-DWITH_ITT=OFF' '-DWITH_TBB=OFF' `
    '-DWITH_OPENMP=OFF' '-DWITH_FFMPEG=OFF' '-DWITH_MSMF=OFF' '-DWITH_GSTREAMER=OFF' `
    '-DWITH_JPEG=OFF' '-DWITH_WEBP=OFF' '-DWITH_TIFF=OFF' '-DWITH_OPENEXR=OFF' `
    '-DWITH_JASPER=OFF' '-DWITH_AVIF=OFF' '-DWITH_JPEGXL=OFF' '-DWITH_OPENJPEG=OFF' `
    '-DWITH_PNG=ON' '-DBUILD_PNG=ON' '-DBUILD_ZLIB=ON' '-DOPENCV_ENABLE_NONFREE=OFF' `
    '-DWITH_PROTOBUF=OFF' '-DBUILD_PROTOBUF=OFF' '-DWITH_ADE=OFF'
if ($LASTEXITCODE -ne 0) { throw '配置 OpenCV 失败。' }

& $Cmake --build $buildOpenCv --parallel 8
if ($LASTEXITCODE -ne 0) { throw '编译 OpenCV 失败。' }

& $Cmake --install $buildOpenCv
if ($LASTEXITCODE -ne 0) { throw '安装 OpenCV 失败。' }

$disabled = 'CONTRIB', 'STITCHING', 'CALIB3D', 'VIDEO', 'FEATURES2D', 'FLANN', 'DNN', 'ML',
    'OBJDETECT', 'PHOTO', 'BARCODE', 'HIGHGUI', 'VIDEOIO', 'INSTALL_TO_TEST' |
    ForEach-Object { "-DNO_$_=ON" }
& $Cmake -S $wrapper -B $buildWrapper @common "-DOpenCV_DIR=$install" @disabled
if ($LASTEXITCODE -ne 0) { throw '配置 OpenCvSharp 封装失败。' }

& $Cmake --build $buildWrapper --parallel 8
if ($LASTEXITCODE -ne 0) { throw '编译 OpenCvSharp 封装失败。' }

$builtDll = Get-ChildItem $buildWrapper -Recurse -Filter 'OpenCvSharpExtern.dll' | Select-Object -First 1
if (-not $builtDll) { throw '没有找到生成的 OpenCvSharpExtern.dll。' }

$resultDir = Join-Path $WorkDir 'result'
New-Item -ItemType Directory -Force -Path $resultDir | Out-Null
$stripped = Join-Path $resultDir 'OpenCvSharpExtern.dll'
& (Join-Path $compilerRoot 'llvm-strip.exe') --strip-unneeded $builtDll.FullName -o $stripped
if ($LASTEXITCODE -ne 0) { throw '裁剪 DLL 失败。' }

$repoDll = Join-Path $PSScriptRoot '../src/MouseClicker/Resources/native/win-x64/OpenCvSharpExtern.dll'
Copy-Item -LiteralPath $stripped -Destination $repoDll -Force
$hash = (Get-FileHash -LiteralPath $repoDll -Algorithm SHA256).Hash
Write-Output "已生成并复制：$repoDll"
Write-Output "SHA256=$hash"
Write-Output '若哈希与预期不同，请同步更新 MouseClicker.csproj 的 MinimalOpenCvLibrarySha256 和 native/README.md。'
