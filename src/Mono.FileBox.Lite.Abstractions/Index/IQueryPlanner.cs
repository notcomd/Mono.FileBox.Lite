// 本文件包含类型 IQueryPlanner：根据可用提供方为查询生成执行计划。
namespace Mono.FileBox.Lite.Abstractions.Index;

/// <summary>Produces an execution plan for a query given the available providers. 中文翻译：根据可用的索引提供方为查询生成执行计划。</summary>
public interface IQueryPlanner
{
    QueryPlan Plan(IndexQuery query);
}