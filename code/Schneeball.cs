using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.ModelEditor.Nodes;
using TerrorTown;


namespace Turrets_Items
{

	[Title( "SchneeBall" )]
	[Category( "Weapons" )]
	[TraitorBuyable( "Weapons", 1, 0 )]

	public class SchneeBall : Gun
	{


		public override string ViewModelPath => "weapons/rust_pistol/v_rust_pistol.vmdl";
		public override string WorldModelPath => "models/snowball.vmdl";

		public override float PrimaryAttackDelay => 0.000f;
		public override float PrimaryReloadDelay => 0.000f;
		public override AmmoType PrimaryAmmoType => AmmoType.None;
		public override float HeadshotMultiplier => 0f;
		public override bool Automatic => false;
		public override bool BulletCasing => false;
		public override bool MuzzleFlash => false;
		public override bool MuzzleFlashLight => false;
		public override void PrimaryAttack()
		{
			PrimaryAmmo -= 1;
			//ShootBullet( 00, 0.0f, 1000 );
			PlaySound( "rust_pistol.shoot" );
			//ShootEffects();
			if ( Game.IsServer )
			{
				(new Snowball()).ThrowMe( Owner.AimRay.Forward, (TerrorTown.Player)Owner );
				this.Delete();
			}

		
		}

		//public override void OnBulletTraceHit( TraceResult trace )
		//{
		//
		//	if ( trace.Entity is TerrorTown.Player )
		//	{
		//		trace.Entity.Components.Add( new SnowmanAttack() );
		//		IEnumerable<SnowmanAttack> attacks = trace.Entity.Components.GetAll<SnowmanAttack>();
		//		foreach ( SnowmanAttack attack in attacks )
		//		{
		// 			attack.Set_Values( (TerrorTown.Player)trace.Entity );
		//		}
		//	}
		//}
	}

	public class Snowball : BasePhysics
	{
		TerrorTown.Player Thrower;
		
		private float start_speed = 1500f;
		private float radius = 50f;
		private List<TerrorTown.Player> AffectedList = new List<TerrorTown.Player>();
		public void ThrowMe( Vector3 dir, TerrorTown.Player ply )
		{
			Thrower = ply;
			Position = ply.AimRay.Forward * 50 + ply.EyeLocalPosition + ply.Position;
			Velocity = dir * start_speed;
			PhysicsBody.DragEnabled = true;
			PhysicsBody.GravityScale = 0.5f;
		}

		public override void Spawn()
		{
			base.Spawn();
			SetModel( "models/snowball.vmdl" );
		}

		protected override ModelPropData GetModelPropData()
		{
			ModelPropData modelPropData = base.GetModelPropData();
			modelPropData.ImpactDamage = 0;
			modelPropData.Health = 100;
			modelPropData.MinImpactDamageSpeed = float.MaxValue;
			return modelPropData;
		}


		public override void Touch( Entity other )
		{
			if(other != Thrower && other.Parent != Thrower )
			{
				Explode();
				Delete();
			}

		}

		private void Explode()
		{
			IEnumerable<Entity> hits =  FindInSphere( Position, radius );
			
			foreach( Entity e in hits )
			{
				if(e != Thrower && e.Parent != Thrower && !AffectedList.Contains(e) && !AffectedList.Contains(e.Parent))
				{
					TerrorTown.Player victim;
					if (e is TerrorTown.Player )
					{
						victim = (TerrorTown.Player)e;
					}

					else if(e.Parent is TerrorTown.Player )
					{
						victim = e.Parent as TerrorTown.Player;
					}
					else
					{
						continue;
					}

					AffectedList.Add( victim );
					victim.Components.Add( new SnowmanAttack());
					IEnumerable<SnowmanAttack> attacks = victim.Components.GetAll<SnowmanAttack>();
					foreach ( SnowmanAttack attack in attacks )
					{
					 	attack.Set_Values(victim);
					}
				}
			}
		}


	}
	public partial class SnowmanAttack : EntityComponent<TerrorTown.Player>
	{
		private TerrorTown.Player victim;
		private RealTimeUntil death;
		private bool killed = false;
		private ModelEntity snowman;
		public SnowmanAttack()
		{
			death = 20;
		}

		public void Set_Values( TerrorTown.Player ply )
		{
			victim = ply;
			victim.PlaySound( "cristmassong" );
		}


		[GameEvent.Tick.Server]
		public void Track_Time()
		{
			Game.AssertServer();
			if ( death )
			{
				if ( !killed )
				{
					victim.Inventory.Items.Clear();
					victim.TakeDamage( new DamageInfo().WithDamage( float.MaxValue ) );

					killed = true;
				}
				if ( killed )
				{
					if ( victim.Corpse != null )
					{
						snowman = new ModelEntity( "models/snowman.vmdl" );
						snowman.Position = victim.Position;
						snowman.Rotation = victim.Rotation;
						victim.Corpse.Delete();
						Entity.Components.Remove( this );

					}
				}


			}

		}
	}

}
