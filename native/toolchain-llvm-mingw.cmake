set(CMAKE_SYSTEM_NAME Windows)
set(CMAKE_SYSTEM_PROCESSOR AMD64)

if(NOT DEFINED ENV{LLVM_MINGW_ROOT})
  message(FATAL_ERROR "请先设置 LLVM_MINGW_ROOT 环境变量，指向 llvm-mingw 解压目录。")
endif()

set(CMAKE_C_COMPILER "$ENV{LLVM_MINGW_ROOT}/bin/clang.exe")
set(CMAKE_CXX_COMPILER "$ENV{LLVM_MINGW_ROOT}/bin/clang++.exe")
