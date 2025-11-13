using Microsoft.AspNetCore.Mvc;
using WGetCrawler.Api.Models;
using WGetCrawler.Api.Services;

namespace WGetCrawler.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly TaskSchedulerService _schedulerService;

    public TasksController(TaskSchedulerService schedulerService)
    {
        _schedulerService = schedulerService;
    }

    /// <summary>
    /// 查询任务状态和结果
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTaskStatus(string id)
    {
        var task = await _schedulerService.GetTaskStatusAsync(id);

        if (task == null)
        {
            return NotFound(new ApiResponse<object>
            {
                Code = 404,
                Message = $"任务不存在: {id}",
                Data = null
            });
        }

        return Ok(new ApiResponse<object>
        {
            Code = 200,
            Message = "成功",
            Data = new
            {
                TaskId = task.TaskId,
                Status = task.Status.ToString().ToLower(),
                TotalCount = task.TotalCount,
                CompletedCount = task.CompletedCount,
                Results = task.Results.Select(r => new
                {
                    ModelNumber = r.ModelNumber,
                    Status = r.Status,
                    Data = r.Data,
                    ErrorMessage = r.ErrorMessage,
                    Duration = r.Duration,
                    RetryCount = r.RetryCount
                })
            }
        });
    }
}
