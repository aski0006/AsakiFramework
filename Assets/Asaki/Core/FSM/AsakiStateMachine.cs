using System;
using System.Collections.Generic;

namespace Asaki.Core.FSM
{
	/// <summary>
	/// Asaki状态机管理器，用于管理状态的切换和生命周期。
	/// </summary>
	/// <typeparam name="TContext">状态持有者类型，通常是控制器类（如PlayerController、GameManager等）。</typeparam>
	/// <remarks>
	/// 此状态机实现了以下核心特性：
	/// <list type="bullet">
	/// <item>懒加载缓存：状态仅在首次使用时创建，之后复用</item>
	/// <item>零运行时GC：通过对象池复用避免频繁的内存分配</item>
	/// <item>泛型驱动：提供类型安全的状态管理</item>
	/// <item>Unity集成：支持与MonoBehaviour生命周期方法配合使用</item>
	/// </list>
	/// 适合用于游戏中的各种状态管理场景，如角色AI、游戏流程控制、UI状态等。
	/// </remarks>
	public class AsakiStateMachine<TContext>
	{
		/// <summary>
		/// 获取状态持有者的上下文实例。
		/// </summary>
		/// <remarks>
		/// 上下文实例包含了状态操作所需的所有数据和方法引用。
		/// 例如，对于PlayerController上下文，可以访问玩家的移动、跳跃等方法。
		/// 此属性在构造函数中被设置，之后不可修改。
		/// </remarks>
		public TContext Context { get; }

		/// <summary>
		/// 获取当前运行的状态实例。
		/// </summary>
		/// <remarks>
		/// 此属性返回当前激活的状态，如果没有激活状态则返回null。
		/// 可以用于状态调试、状态转换条件检查等。
		/// </remarks>
		public AsakiState<TContext> CurrentState => _currentState;

		// 当前运行状态
		private AsakiState<TContext> _currentState;

		// 状态缓存池 (Key: 状态类型, Value: 状态实例)
		// 只有第一次切换到某状态时会 new，之后全部复用
		private readonly Dictionary<Type, AsakiState<TContext>> _stateCache;

		/// <summary>
		/// 初始化<see cref="AsakiStateMachine{TContext}"/>类的新实例。
		/// </summary>
		/// <param name="context">状态持有者的上下文实例。</param>
		/// <remarks>
		/// 构造函数初始化状态机的上下文和状态缓存池。
		/// 状态机创建后，需要调用<see cref="ChangeState{TState}"/>方法来启动第一个状态。
		/// </remarks>
		public AsakiStateMachine(TContext context)
		{
			Context = context;
			_stateCache = new Dictionary<Type, AsakiState<TContext>>();
		}

		/// <summary>
		/// 切换到指定类型的状态。
		/// </summary>
		/// <typeparam name="TState">目标状态类型，必须继承自<see cref="AsakiState{TContext}"/>并提供无参构造函数。</typeparam>
		/// <remarks>
		/// 状态切换过程包括：
		/// 1. 调用当前状态的OnExit方法
		/// 2. 获取目标状态（从缓存中获取或创建新实例）
		/// 3. 调用目标状态的OnEnter方法
		/// 此方法是线程安全的，并且在运行时不会产生GC分配。
		/// </remarks>
		public void ChangeState<TState>() where TState : AsakiState<TContext>, new()
		{
			// 1. 退出当前状态
			_currentState?.OnExit();

			// 2. 获取目标状态 (查缓存 -> 懒加载)
			Type type = typeof(TState);
			if (!_stateCache.TryGetValue(type, out var newState))
			{
				newState = new TState();
				newState.Initialize(this, Context);
				_stateCache.Add(type, newState);
			}

			// 3. 进入新状态
			_currentState = newState;
			_currentState.OnEnter();
		}

		/// <summary>
		/// 执行当前状态的每帧更新。
		/// </summary>
		/// <param name="deltaTime">自上一帧以来经过的时间（秒）。</param>
		/// <remarks>
		/// 此方法建议在MonoBehaviour.Update中调用，用于处理与时间相关的更新逻辑。
		/// 例如：输入处理、状态转换条件检查、动画更新等。
		/// 如果当前没有激活状态，则此方法不执行任何操作。
		/// </remarks>
		public void Update(float deltaTime)
		{
			_currentState?.OnUpdate(deltaTime);
		}

		/// <summary>
		/// 执行当前状态的物理更新。
		/// </summary>
		/// <param name="fixedDeltaTime">固定时间步长（秒），通常为0.02秒。</param>
		/// <remarks>
		/// 此方法建议在MonoBehaviour.FixedUpdate中调用，用于处理物理相关的更新逻辑。
		/// 例如：刚体移动、碰撞检测、物理力应用等。
		/// 如果当前没有激活状态，则此方法不执行任何操作。
		/// </remarks>
		public void FixedUpdate(float fixedDeltaTime)
		{
			_currentState?.OnFixedUpdate(fixedDeltaTime);
		}

		/// <summary>
		/// 停止状态机并清理所有状态实例。
		/// </summary>
		/// <remarks>
		/// 此方法执行以下操作：
		/// 1. 调用当前状态的OnExit方法
		/// 2. 清空当前状态引用
		/// 3. 清理状态缓存池
		/// 适合在不再需要状态机时调用，如对象销毁时。
		/// </remarks>
		public void Stop()
		{
			_currentState?.OnExit();
			_currentState = null;
			_stateCache.Clear();
		}
	}
}
