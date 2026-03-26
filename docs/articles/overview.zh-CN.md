# 中文概览

`TileWorld.Engine` 是一个以分块 Tile 世界为核心的 2D 引擎。

当前文档站已经接入 DocFX，并通过 GitHub Actions 具备发布到 GitHub Pages 的能力。

## 当前文档范围

目前站点覆盖的内容包括：

- 引擎整体设计理念
- 运行时与宿主层的边界
- 程序化世界、存储与流式基础
- 本地构建、测试与 Desktop 沙盒运行方式
- GitHub Pages 发布流程
- 引擎公开 API 参考

## 当前实现阶段

项目已经不再停留在最初的启动里程碑，而是已经进入第四阶段起步：

- 已有稳定的 Chunk-first 世界运行时
- 已有 Tile / Wall / Object / Entity 原型链路
- 已有 Desktop 存档选择与世界内交互壳
- 已接入程序化世界生成、Biome 查询、Chunk 来源跟踪、Chunk 预取基础

## 核心设计思想

- `Tile-first`：Tile Cell 是最小交互单元
- `Chunk-first`：Chunk 是最小的加载、保存、脏标记和渲染缓存单元
- `Facade-first`：外部优先通过 `WorldRuntime` 使用引擎
- `Backend-decoupled`：核心引擎不暴露 MonoGame 类型

## 快速入口

- [Getting Started](getting-started.md)
- [Architecture Overview](architecture.md)
- [GitHub Pages Publishing](github-pages.md)
- [API Reference](../api/index.md)

更完整的仓库说明可以直接查看：

- [README.md](https://github.com/ZQDesigned/TileWorldEngine/blob/master/README.md)
- [README.zh-CN.md](https://github.com/ZQDesigned/TileWorldEngine/blob/master/README.zh-CN.md)
