# 精简 OpenCV 原生库

发布时（`win-x64`）会用仓库内置的 `src/MouseClicker/Resources/native/win-x64/OpenCvSharpExtern.dll`
替换 `OpenCvSharp4.runtime.win.slim` 自带的官方原生库，以减小便携版、安装包和单文件 exe 的体积。
替换逻辑在 `src/MouseClicker/MouseClicker.csproj` 的 `_UseMinimalOpenCvNativeLibrary` 目标中，
构建时会校验 SHA256，发布工作流还会再次校验产物。

## 版本与来源

| 项目 | 值 |
| --- | --- |
| OpenCvSharp | `b161e7e012f5101f6d5dc68a835c59db6cc88b18`（对应 NuGet 4.13.0.20260627） |
| OpenCV | `fe38fc608f6acb8b68953438a62305d8318f4fcd`（4.13.0） |
| 编译器 | LLVM-MinGW 20260616（Clang 22.1.8），UCRT x64 |
| CMake / Ninja | 3.31.8 / 1.12.1 |
| 链接方式 | OpenCV 与 C++ 运行时静态链接，仅依赖 Windows 系统 DLL |

## 保留与裁剪

- 保留模块：`core`、`imgproc`、`imgcodecs`。
- 保留编解码：PNG、BMP（PNG 自带 zlib）。
- 保留 CPU 指令集基线 SSE/SSE2/SSE3，并分发 SSE4.1/SSE4.2/AVX/FP16/AVX2/AVX512_SKX，与官方库一致。
- 关闭：IPP、OpenCL、ITT、TBB、OpenMP、FFmpeg、JPEG、WebP、TIFF、OpenEXR、AVIF、JXL，以及
  calib3d/features2d/flann/objdetect/photo/ml/dnn/videoio/highgui 等模块。

程序目前只使用模板匹配（`imgproc`）和 PNG 编解码（`imgcodecs`），因此该库满足现有功能。
如果以后新增了其他 OpenCV 模块或图片格式，需要重新构建并评估。

## 体积

- 精简库：11,367,936 字节（10.84 MiB），SHA256 `DBB174C553BEAB60A2FD94BAF620C79AF0ABD57A95826F3EF512C6DA57C71991`
- 官方库：55,547,904 字节（52.97 MiB），SHA256 `1FA122BDB8E94175E7719FB8AA8F2AB211268A756F5D0C7A13C710ED79AE30CD`
- 自包含单文件 exe 压缩后约减少 16.3 MB（72.8 MB → 56.5 MB）。

## 验证

- 把精简库复制到测试输出目录后，完整 xUnit 套件 515 项全部通过（包含模板匹配与 PNG 编解码用例）。
- 单文件发布包做过启动、正常关闭冒烟，并确认包内解压出的原生库哈希与本文一致。
- 构建和发布流程会校验库文件哈希，防止误用官方库或损坏文件。

## 重新构建

准备源码与工具到 `native/work`（该目录已被忽略，不会提交）：

```powershell
# 源码
git clone https://github.com/opencv/opencv.git native/work/src/opencv-fe38fc608f6acb8b68953438a62305d8318f4fcd
git -C native/work/src/opencv-fe38fc608f6acb8b68953438a62305d8318f4fcd checkout fe38fc608f6acb8b68953438a62305d8318f4fcd
git clone https://github.com/shimat/opencvsharp.git native/work/src/opencvsharp-b161e7e012f5101f6d5dc68a835c59db6cc88b18
git -C native/work/src/opencvsharp-b161e7e012f5101f6d5dc68a835c59db6cc88b18 checkout b161e7e012f5101f6d5dc68a835c59db6cc88b18

# 工具（版本见上表），解压到：
#   native/work/tools/llvm-mingw-20260616-ucrt-x86_64/
#   native/work/tools/cmake-3.31.8-windows-x86_64/
#   native/work/tools/ninja/
powershell -ExecutionPolicy Bypass -File native/build-native-simd.ps1
```

脚本会把裁剪后的 DLL 复制到 `src/MouseClicker/Resources/native/win-x64/` 并输出 SHA256。
如果哈希变化，请同步更新 `MouseClicker.csproj` 中的 `MinimalOpenCvLibrarySha256` 和本文档。

## 许可证

该 DLL 包含 OpenCV 与 OpenCvSharp 的代码（Apache License 2.0），并静态链接 zlib、
libpng 与 LLVM libc++/libunwind。发布包会把相关许可证文本复制到 `licenses` 目录，
清单见 [licenses/THIRD-PARTY-NOTICES.txt](licenses/THIRD-PARTY-NOTICES.txt)：

- [LICENSE-OpenCV.txt](licenses/LICENSE-OpenCV.txt)
- [LICENSE-OpenCvSharp.txt](licenses/LICENSE-OpenCvSharp.txt)
- [LICENSE-zlib.txt](licenses/LICENSE-zlib.txt)
- [LICENSE-libpng.txt](licenses/LICENSE-libpng.txt)
- [LICENSE-LLVM.txt](licenses/LICENSE-LLVM.txt)
- [LICENSE-dotnet-runtime.txt](licenses/LICENSE-dotnet-runtime.txt)
