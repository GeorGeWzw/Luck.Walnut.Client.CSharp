﻿using Grpc.Net.Client;
using Luck.Walnut.V1;

namespace Luck.Walnut.Client
{
    public class LuckWalnutConfigCenterHelper
    {

        private ManualResetEventSlim _manualResetEventSlim;
        private readonly LuckWalnutConfig _luckWalnutConfig;
        private string _serverUri;
        private IDictionary<string, IDictionary<string, string>> _projectsConfigs;
        private readonly ILuckWalnutSourceManager _luckWalnutSourceManager;

        public LuckWalnutConfigCenterHelper(LuckWalnutConfig luckWalnutConfig)
        {
            _manualResetEventSlim = new ManualResetEventSlim(false);
            _luckWalnutConfig= luckWalnutConfig;
            _luckWalnutSourceManager = new LuckWalnutSourceManager(_luckWalnutConfig);
            _projectsConfigs = new BlockingDictionary<string, IDictionary<string, string>>();
        }

        private void SetProjectsConfigs(IEnumerable<LuckWalnutConfigAdapter> luckWalnutConfigs)
        {
            try
            {
                var configDic = luckWalnutConfigs.ToDictionary(config => config.Key, config => config.Value);

                _projectsConfigs[_luckWalnutConfig.AppId] = configDic;
            }
            catch (Exception es)
            {

                throw;
            }

        }

        private void GetProjectConfigs()
        {
            var task = Task.Factory.StartNew(async () =>
              {
                  try
                  {
                      Exception? exception = null;
                      IEnumerable<LuckWalnutConfigAdapter>? projectsConfigs = null;

                      for (int i = 0; i < 5; i++)  //尝试多次，防止单次运行出错
                      {
                          try
                          {
                              projectsConfigs = await _luckWalnutSourceManager.GetProjectConfigs();
                              break;
                          }
                          catch (Exception ex)
                          {
                              exception = ex;
                          }
                      }
                      if (projectsConfigs == null)
                      {
                          //Log.Error(exception, "统一配置获取失败");
                          throw exception;
                      }
                      SetProjectsConfigs(projectsConfigs);
                  }

                  finally
                  {
                      _manualResetEventSlim.Set();
                  }


              });

            _manualResetEventSlim.Wait();
            task.Wait();
        }

        public IDictionary<string, IDictionary<string, string>> GetConfig()
        {

            GetProjectConfigs();

            return _projectsConfigs;
        }

    }
}