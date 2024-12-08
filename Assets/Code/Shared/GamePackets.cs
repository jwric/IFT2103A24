using System;
using LiteNetLib.Utils;
using UnityEngine;

namespace Code.Shared
{
    public enum PacketType : byte
    {
        Movement,
        Spawn,
        ServerState,
        Serialized,
        Shoot,
        PlayerDeath,
    }
    
    //Auto serializable packets
    public class JoinPacket
    {
        public string UserName { get; set; }
    }

    public class JoinAcceptPacket
    {
        public byte Id { get; set; }
        public ushort ServerTick { get; set; }
    }

    public class PlayerJoinedPacket
    {
        public string UserName { get; set; }
        public bool NewPlayer { get; set; }
        public byte Health { get; set; }
        public ushort ServerTick { get; set; }
        public PlayerState InitialPlayerState { get; set; }
    }

    public class PlayerLeavedPacket
    {
        public byte Id { get; set; }
    }

    //Manual serializable packets
    public struct SpawnPacket : INetSerializable
    {
        public byte PlayerId;
        public Vector2 Position;
        
        public void Serialize(NetDataWriter writer)
        {
            writer.Put(PlayerId);
            writer.Put(Position);
        }

        public void Deserialize(NetDataReader reader)
        {
            PlayerId = reader.GetByte();
            Position = reader.GetVector2();
        }
    }

    [Flags]
    public enum MovementKeys : byte
    {
        Left = 1 << 1,
        Right = 1 << 2,
        Up = 1 << 3,
        Down = 1 << 4,
        Fire = 1 << 5
    }

    public struct ShootPacket : INetSerializable
    {
        public byte FromPlayer;
        public ushort CommandId;
        public Vector2 Hit;
        public bool AnyHit;
        public byte PlayerHit;
        public ushort ServerTick;
        
        public void Serialize(NetDataWriter writer)
        {
            writer.Put(FromPlayer);
            writer.Put(CommandId);
            writer.Put(Hit);
            writer.Put(AnyHit);
            writer.Put(PlayerHit);
            writer.Put(ServerTick);
        }

        public void Deserialize(NetDataReader reader)
        {
            FromPlayer = reader.GetByte();
            CommandId = reader.GetUShort();
            Hit = reader.GetVector2();
            AnyHit = reader.GetBool();
            PlayerHit = reader.GetByte();
            ServerTick = reader.GetUShort();
        }
    }
    
    public struct PlayerDeathPacket : INetSerializable
    {
        public byte Id;
        public byte KilledBy;
        public ushort ServerTick;
        
        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(KilledBy);
            writer.Put(ServerTick);
        }

        public void Deserialize(NetDataReader reader)
        {
            Id = reader.GetByte();
            KilledBy = reader.GetByte();
            ServerTick = reader.GetUShort();
        }
    }

    public struct PlayerInputPacket : INetSerializable
    {
        public ushort Id;
        public MovementKeys Keys;
        public Vector2 Thrust;
        public float AngularThrust;
        public ushort ServerTick;
        public float Delta;
        public float Time;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put((byte)Keys);
            writer.Put(Thrust);
            writer.Put(AngularThrust);
            writer.Put(ServerTick);
            writer.Put(Delta);
            writer.Put(Time);
        }

        public void Deserialize(NetDataReader reader)
        {
            Id = reader.GetUShort();
            Keys = (MovementKeys)reader.GetByte();
            Thrust = reader.GetVector2();
            AngularThrust = reader.GetFloat();
            ServerTick = reader.GetUShort();
            Delta = reader.GetFloat();
            Time = reader.GetFloat();
        }
    }
    
    public struct PlayerState : INetSerializable
    {
        public byte Id;
        public Vector2 Position;
        public Vector2 Velocity;
        public float Rotation;
        public float AngularVelocity;
        public ushort Tick;
        public float Time;
        public byte Health;

        public const int Size = sizeof(byte) + sizeof(float)*7 + sizeof(ushort) + sizeof(byte);
        
        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(Position);
            writer.Put(Velocity);
            writer.Put(Rotation);
            writer.Put(AngularVelocity);
            writer.Put(Tick);
            writer.Put(Time);
            writer.Put(Health);
        }

        public void Deserialize(NetDataReader reader)
        {
            Id = reader.GetByte();
            Position = reader.GetVector2();
            Velocity = reader.GetVector2();
            Rotation = reader.GetFloat();
            AngularVelocity = reader.GetFloat();
            Tick = reader.GetUShort();
            Time = reader.GetFloat();
            Health = reader.GetByte();
        }
    }
    
    public struct PhysicsEntityState : INetSerializable
    {
        public byte Id;
        public Vector2 Position;
        public Vector2 Velocity;
        public float Rotation;
        public float AngularVelocity;
        public ushort Tick;

        public const int Size = sizeof(float)*6 + sizeof(ushort);
        
        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Position);
            writer.Put(Velocity);
            writer.Put(Rotation);
            writer.Put(AngularVelocity);
            writer.Put(Tick);
        }

        public void Deserialize(NetDataReader reader)
        {
            Position = reader.GetVector2();
            Velocity = reader.GetVector2();
            Rotation = reader.GetFloat();
            AngularVelocity = reader.GetFloat();
            Tick = reader.GetUShort();
        }
    }

    public struct ServerState : INetSerializable
    {
        public ushort Tick;
        public ushort LastProcessedCommand;
        
        public int PlayerStatesCount;
        public int StartState; //server only
        public PlayerState[] PlayerStates;
        
        public int PhysicsEntityStatesCount;
        public PhysicsEntityState[] PhysicsEntityStates;
        
        //tick
        public const int HeaderSize = sizeof(ushort)*2;
        
        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Tick);
            writer.Put(LastProcessedCommand);
            
            writer.Put(PlayerStatesCount);
            for (int i = 0; i < PlayerStatesCount; i++)
                PlayerStates[StartState + i].Serialize(writer);
            
            writer.Put(PhysicsEntityStatesCount);
            for (int i = 0; i < PhysicsEntityStatesCount; i++)
                PhysicsEntityStates[i].Serialize(writer);
        }

        public void Deserialize(NetDataReader reader)
        {
            Tick = reader.GetUShort();
            LastProcessedCommand = reader.GetUShort();
            
            PlayerStatesCount = reader.GetInt();
            if (PlayerStates == null || PlayerStates.Length < PlayerStatesCount)
                PlayerStates = new PlayerState[PlayerStatesCount];
            for (int i = 0; i < PlayerStatesCount; i++)
                PlayerStates[i].Deserialize(reader);
            
            PhysicsEntityStatesCount = reader.GetInt();
            if (PhysicsEntityStates == null || PhysicsEntityStates.Length < PhysicsEntityStatesCount)
                PhysicsEntityStates = new PhysicsEntityState[PhysicsEntityStatesCount];
        }
    }
}