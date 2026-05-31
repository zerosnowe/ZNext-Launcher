using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ZNext.Services
{
    public class NodeService
    {
        private const string ApiBaseUrl = "https://api.mefrp.com/api";
        private const string NodeListApiUrl = ApiBaseUrl + "/auth/node/list";
        private const string NodeStatusApiUrl = ApiBaseUrl + "/auth/node/status";
        private readonly IHttpService _httpService;
        private string? _bearerToken;

        public NodeService() : this(new HttpService())
        {
        }

        public NodeService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public void SetToken(string token)
        {
            _bearerToken = token;
            _httpService.SetAuthToken(token);
        }

        public void ClearToken()
        {
            _bearerToken = null;
            _httpService.ClearAuthToken();
        }

        public async Task<NodeListResult> GetNodeListAsync()
        {
            try
            {
                using var response = await _httpService.GetAsync(NodeListApiUrl);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new NodeListResult
                    {
                        Success = false,
                        Message = $"获取节点列表失败: HTTP {(int)response.StatusCode}",
                        Error = responseContent
                    };
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var apiResponse = JsonSerializer.Deserialize<NodeApiResponse>(responseContent, options);
                if (apiResponse == null)
                {
                    return new NodeListResult
                    {
                        Success = false,
                        Message = "节点列表解析失败",
                        Error = responseContent
                    };
                }

                if (apiResponse.Code != 200)
                {
                    return new NodeListResult
                    {
                        Success = false,
                        Message = apiResponse.Message,
                        Error = responseContent
                    };
                }

                return new NodeListResult
                {
                    Success = true,
                    Message = apiResponse.Message,
                    Nodes = apiResponse.Data ?? new List<NodeInfo>()
                };
            }
            catch (Exception ex)
            {
                return new NodeListResult
                {
                    Success = false,
                    Message = "获取节点列表异常",
                    Error = ex.Message
                };
            }
        }

        public async Task<NodeStatusResult> GetNodeStatusAsync()
        {
            try
            {
                // API doc: GET /api/auth/node/status + Authorization: Bearer <token>
                using var response = await _httpService.GetAsync(NodeStatusApiUrl);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new NodeStatusResult
                    {
                        Success = false,
                        Message = $"获取节点状态失败: HTTP {(int)response.StatusCode}",
                        Error = responseContent
                    };
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var apiResponse = JsonSerializer.Deserialize<StatusApiResponse>(responseContent, options);
                if (apiResponse == null)
                {
                    return new NodeStatusResult
                    {
                        Success = false,
                        Message = "节点状态解析失败",
                        Error = responseContent
                    };
                }

                if (apiResponse.Code != 200)
                {
                    return new NodeStatusResult
                    {
                        Success = false,
                        Message = apiResponse.Message,
                        Error = responseContent
                    };
                }

                return new NodeStatusResult
                {
                    Success = true,
                    Message = apiResponse.Message,
                    Statuses = apiResponse.Data ?? new List<NodeStatus>()
                };
            }
            catch (Exception ex)
            {
                return new NodeStatusResult
                {
                    Success = false,
                    Message = "获取节点状态异常",
                    Error = ex.Message
                };
            }
        }

        public async Task<List<NodeInfoWithStatus>> GetNodesWithStatusAsync()
        {
            var statusTask = GetNodeStatusAsync();
            var listTask = GetNodeListAsync();
            await Task.WhenAll(statusTask, listTask);

            var statusResult = await statusTask;
            var listResult = await listTask;
            var listByNodeId = (listResult.Success ? listResult.Nodes : new List<NodeInfo>())
                ?.ToDictionary(n => n.NodeId, n => n) ?? new Dictionary<int, NodeInfo>();

            // Follow dashboard/node-status frontend behavior:
            // it requests only /auth/node/status for monitoring data.
            if (!statusResult.Success)
            {
                Debug.WriteLine($"GetNodeStatusAsync failed: {statusResult.Message}");
                return new List<NodeInfoWithStatus>();
            }

            var statuses = statusResult.Statuses ?? new List<NodeStatus>();
            var onlineNodes = new List<NodeInfoWithStatus>();
            var offlineNodes = new List<NodeInfoWithStatus>();

            foreach (var status in statuses.OrderBy(s => s.NodeId))
            {
                var nodeInfo = BuildNodeInfoFromStatus(status);
                if (listByNodeId.TryGetValue(status.NodeId, out var detailedInfo))
                {
                    nodeInfo = MergeNodeInfo(nodeInfo, detailedInfo);
                }

                var item = new NodeInfoWithStatus
                {
                    NodeInfo = nodeInfo,
                    NodeStatus = status,
                    OnlineStatusText = BuildOnlineStatusText(status)
                };

                if (status.IsOnline)
                {
                    onlineNodes.Add(item);
                }
                else
                {
                    offlineNodes.Add(item);
                }
            }

            var result = new List<NodeInfoWithStatus>(onlineNodes.Count + offlineNodes.Count);
            result.AddRange(onlineNodes);
            result.AddRange(offlineNodes);

            Debug.WriteLine($"Nodes loaded from /auth/node/status + /auth/node/list: total={result.Count}, online={onlineNodes.Count}, offline={offlineNodes.Count}");
            return result;
        }

        private static NodeInfo MergeNodeInfo(NodeInfo baseInfo, NodeInfo detailedInfo)
        {
            return new NodeInfo
            {
                NodeId = baseInfo.NodeId,
                Name = string.IsNullOrWhiteSpace(detailedInfo.Name) ? baseInfo.Name : detailedInfo.Name,
                Hostname = detailedInfo.Hostname,
                Description = detailedInfo.Description,
                Token = detailedInfo.Token,
                ServicePort = detailedInfo.ServicePort,
                AdminPort = detailedInfo.AdminPort,
                AdminPass = detailedInfo.AdminPass,
                AllowGroup = detailedInfo.AllowGroup,
                AllowPort = detailedInfo.AllowPort,
                AllowType = detailedInfo.AllowType,
                Region = detailedInfo.Region,
                Bandwidth = detailedInfo.Bandwidth,
                IsOnline = baseInfo.IsOnline,
                IsDisabled = detailedInfo.IsDisabled,
                TotalTrafficIn = baseInfo.TotalTrafficIn,
                TotalTrafficOut = baseInfo.TotalTrafficOut,
                UpTime = baseInfo.UpTime,
                Version = string.IsNullOrWhiteSpace(baseInfo.Version) ? detailedInfo.Version : baseInfo.Version
            };
        }

        private static NodeInfo BuildNodeInfoFromStatus(NodeStatus status)
        {
            return new NodeInfo
            {
                NodeId = status.NodeId,
                Name = status.Name,
                Description = string.Empty,
                Region = string.Empty,
                Bandwidth = string.Empty,
                AllowGroup = string.Empty,
                IsOnline = status.IsOnline,
                TotalTrafficIn = status.TotalTrafficIn,
                TotalTrafficOut = status.TotalTrafficOut,
                UpTime = status.UpTime,
                Version = status.Version
            };
        }

        private static string BuildOnlineStatusText(NodeStatus status)
        {
            if (status == null || status.UpTime <= 0)
            {
                return "未知";
            }

            var timeText = FormatDuration(status.UpTime);
            return status.IsOnline ? $"已在线 {timeText}" : $"{timeText} 前离线";
        }

        private static string FormatDuration(long seconds)
        {
            if (seconds < 60)
            {
                return $"{seconds}秒";
            }

            if (seconds < 3600)
            {
                return $"{seconds / 60}分钟";
            }

            if (seconds < 86400)
            {
                var hours = seconds / 3600;
                var minutes = (seconds % 3600) / 60;
                return $"{hours}小时{minutes}分钟";
            }

            var days = seconds / 86400;
            var remainHours = (seconds % 86400) / 3600;
            return $"{days}天{remainHours}小时";
        }
    }

    public class NodeApiResponse
    {
        public int Code { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<NodeInfo>? Data { get; set; }
    }

    public class StatusApiResponse
    {
        public int Code { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<NodeStatus>? Data { get; set; }
    }

    public class NodeListResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Error { get; set; }
        public List<NodeInfo>? Nodes { get; set; }
    }

    public class NodeStatusResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Error { get; set; }
        public List<NodeStatus>? Statuses { get; set; }
    }

    public class NodeInfo
    {
        public int NodeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Hostname { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public int ServicePort { get; set; }
        public int AdminPort { get; set; }
        public string AdminPass { get; set; } = string.Empty;
        public string AllowGroup { get; set; } = string.Empty;
        public string AllowPort { get; set; } = string.Empty;
        public string AllowType { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string Bandwidth { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
        public bool IsDisabled { get; set; }
        public long TotalTrafficIn { get; set; }
        public long TotalTrafficOut { get; set; }
        public long UpTime { get; set; }
        public string Version { get; set; } = string.Empty;
    }

    public class NodeStatus
    {
        public int NodeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public long TotalTrafficIn { get; set; }
        public long TotalTrafficOut { get; set; }
        public int OnlineClient { get; set; }
        public int OnlineProxy { get; set; }
        public bool IsOnline { get; set; }
        public string Version { get; set; } = string.Empty;
        public long UpTime { get; set; }
        public int CurConns { get; set; }
        public int LoadPercent { get; set; }
    }

    public class NodeInfoWithStatus
    {
        public NodeInfo NodeInfo { get; set; } = new NodeInfo();
        public NodeStatus? NodeStatus { get; set; }
        public string OnlineStatusText { get; set; } = "未知";
    }
}
