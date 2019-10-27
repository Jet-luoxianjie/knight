﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Knight.Core;

namespace Knight.Hotfix.Core
{
    public interface IHotfixKnightObject : IDisposable
    {
        Task Initialize();
        void Update(float fDeltaTime);
    }

    public class KnightEvent
    {
        public int                  EventCode;
        public Action<EventArg>     EventHandler;
    }

    public class THotfixKnightObject<T> : IHotfixKnightObject where T : IHotfixKnightObject
    {
        protected List<KnightEvent> mEvents;

#pragma warning disable 1998
        public virtual async Task Initialize()
#pragma warning restore 1998
        {
            // 解析事件Attribute            
            this.mEvents = new List<KnightEvent>();

            var rType = this.GetType();
            if (rType == null) return;

            var rBindingFlags = BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;
            var rMethodInfos = rType.GetMethods(rBindingFlags);
            for (int i = 0; i < rMethodInfos.Length; i++)
            {
                var rMethodInfo = rMethodInfos[i];

                // 普通消息
                var rAttrObjs = rMethodInfo.GetCustomAttributes(typeof(HotfixEventAttribute), false);
                if (rAttrObjs != null && rAttrObjs.Length > 0)
                {
                    var rEventAttr = rAttrObjs[0] as HotfixEventAttribute;
                    if (rEventAttr != null)
                    {
                        Action<EventArg> rActionDelegate = (rEventArgs) => { rMethodInfo.Invoke(this, new object[] { rEventArgs }); };
                        var rKnightEvent = new KnightEvent()
                        {
                            EventCode = rEventAttr.MsgCode,
                            EventHandler = rActionDelegate
                        };
                        this.mEvents.Add(rKnightEvent);
                        // 绑定事件
                        EventManager.Instance.Binding(rKnightEvent.EventCode, rKnightEvent.EventHandler);
                    }
                }

                // 网络消息
                rAttrObjs = rMethodInfo.GetCustomAttributes(typeof(HotfixMessageHandlerAttribute), false);
                if (rAttrObjs != null && rAttrObjs.Length > 0)
                {
                    var rNetEventAttr = rAttrObjs[0] as HotfixMessageHandlerAttribute;
                    if (rNetEventAttr != null)
                    {
                        Action<EventArg> rActionDelegate = (rEventArgs) => { rMethodInfo.Invoke(this, new object[] { rEventArgs }); };
                        var rKnightEvent = new KnightEvent()
                        {
                            EventCode = rNetEventAttr.Opcode,
                            EventHandler = rActionDelegate
                        };
                        this.mEvents.Add(rKnightEvent);
                        // 绑定网络消息
                        EventManager.Instance.Binding(rKnightEvent.EventCode, rKnightEvent.EventHandler);
                    }
                }
            }
        }

        public virtual void Update(float fDeltaTime)
        {
        }

        public virtual void Dispose()
        {
            for (int i = 0; i < this.mEvents.Count; i++)
            {
                // 解绑事件
                EventManager.Instance.Unbinding(this.mEvents[i].EventCode, this.mEvents[i].EventHandler);
                this.mEvents[i].EventHandler = null;
            }
            this.mEvents.Clear();
        }
    }

    public class HotfixKnightObject : THotfixKnightObject<HotfixKnightObject>
    {
        public sealed override async Task Initialize()
        {
            await base.Initialize();
            await this.OnInitialize();
        }

        public sealed override void Update(float fDeltaTime)
        {
            base.Update(fDeltaTime);
            this.OnUpdate(fDeltaTime);
        }

        public sealed override void Dispose()
        {
            this.OnDispose();
            base.Dispose();
        }

#pragma warning disable 1998
        protected virtual async Task OnInitialize()
#pragma warning restore 1998
        {
        }

        protected virtual void OnUpdate(float fDeltaTime)
        {
        }

        protected virtual void OnDispose()
        {
        }
    }
}
