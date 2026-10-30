using Xunit;

// 测试会修改全局界面语言并共享 WinForms 静态状态，串行执行可避免相互干扰。
[assembly: CollectionBehavior(DisableTestParallelization = true)]
