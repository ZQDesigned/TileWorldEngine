# 中文概览

`TileWorld.Engine` 是一个以分块 Tile 世界为核心的 2D 引擎。

当前文档站点已经接入了 DocFX，并通过 GitHub Actions 具备发布到 GitHub Pages 的能力。

## 当前文档站点包含的内容

- 引擎整体设计理念
- 本地构建、测试与运行方式
- GitHub Pages 发布流程
- 引擎公开 API 参考

## 核心设计思想

- `Tile-first`：Tile Cell 是最小交互单元
- `Chunk-first`：Chunk 是最小加载、保存、脏标记和渲染缓存单元
- `Facade-first`：外部应优先通过 `WorldRuntime` 使用引擎
- `Backend-decoupled`：核心引擎不暴露 MonoGame 类型

## 快速入口

- [Getting Started](getting-started.md)
- [Architecture Overview](architecture.md)
- [GitHub Pages Publishing](github-pages.md)
- [API Reference](../api/index.md)

更完整的仓库说明仍然可以直接查看：

- [README.md](https://github.com/ZQDesigned/TileWorldEngine/blob/master/README.md)
- [README.zh-CN.md](https://github.com/ZQDesigned/TileWorldEngine/blob/master/README.zh-CN.md)
