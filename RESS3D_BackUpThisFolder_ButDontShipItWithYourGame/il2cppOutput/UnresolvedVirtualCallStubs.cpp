#include "il2cpp-config.h"

#ifndef _MSC_VER
# include <alloca.h>
#else
# include <malloc.h>
#endif


#include <stdint.h>

#include "codegen/il2cpp-codegen.h"
#include "il2cpp-object-internals.h"


// Mirror.NetworkConnection
struct NetworkConnection_tB7F48309DFDE730F2B8365840A48DFF388C8D553;
// Mirror.NetworkReader
struct NetworkReader_tAA88A75EFC73573BCCA5F501C814190B01ED4C25;
// Mono.Unity.UnityTls/unitytls_tlsctx_read_callback
struct unitytls_tlsctx_read_callback_tD85E7923018681355C1D851137CEC527F04093F5;
// Mono.Unity.UnityTls/unitytls_tlsctx_write_callback
struct unitytls_tlsctx_write_callback_tBDF40F27E011F577C3E834B14788491861F870D6;
// System.Action`1<System.Object>
struct Action_1_t551A279CEADCF6EEAE8FA2B1E1E757D0D15290D0;
// System.Byte[]
struct ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821;
// System.Char[]
struct CharU5BU5D_t4CC6ABF0AD71BEC97E3C2F1E9C5677E46D3A75C2;
// System.Int32[]
struct Int32U5BU5D_t2B9E4FDDDB9F0A00EC0AC631BA2DA915EB1ECF83;
// System.Reflection.MemberInfo
struct MemberInfo_t;
// System.Security.Cryptography.RandomNumberGenerator
struct RandomNumberGenerator_t12277F7F965BA79C54E4B3BFABD27A5FFB725EE2;
// System.String
struct String_t;
// System.Threading.CancellationTokenSource
struct CancellationTokenSource_tF480B7E74A032667AFBD31F0530D619FB43AD3FE;
// System.Threading.ManualResetEvent
struct ManualResetEvent_tDFAF117B200ECA4CCF4FD09593F949A016D55408;
// System.Threading.SendOrPostCallback
struct SendOrPostCallback_t3F9C0164860E4AA5138DF8B4488DFB0D33147F01;
// System.Type
struct Type_t;
// System.UInt32[]
struct UInt32U5BU5D_t9AA834AF2940E75BBF8E3F08FF0D20D266DB71CB;
// System.Void
struct Void_t22962CB4C05B1D89B55A6E1139F0E87A90987017;
// UnityEngine.EventSystems.BaseRaycaster
struct BaseRaycaster_tC7F6105A89F54A38FBFC2659901855FDBB0E3966;
// UnityEngine.Events.UnityAction
struct UnityAction_tD19B26F1B2C048E38FD5801A33573BE01064CAF4;
// UnityEngine.Experimental.XR.XRCameraSubsystem
struct XRCameraSubsystem_t9271DB5D8FEDD3431246FCB6D9257A940246E701;
// UnityEngine.Experimental.XR.XRDepthSubsystem
struct XRDepthSubsystem_t1F42ECBC6085EFA6909640A9521920C55444D089;
// UnityEngine.Experimental.XR.XRGestureSubsystem
struct XRGestureSubsystem_t3B96ED0176B1F522E14CFC2C44F688F8FC3FAA7D;
// UnityEngine.Experimental.XR.XRPlaneSubsystem
struct XRPlaneSubsystem_t3ABC3FDCBC5AC6F1CAA30E08A89EFD7F9D49D72B;
// UnityEngine.Experimental.XR.XRSessionSubsystem
struct XRSessionSubsystem_t75B8ED54B2BF4876D83B93780A7E13D6A9F32B8F;
// UnityEngine.GameObject
struct GameObject_tBD1244AD56B4E59AAD76E5E7C9282EC5CE434F0F;
// UnityEngine.Mesh
struct Mesh_t6106B8D8E4C691321581AB0445552EC78B947B8C;
// UnityEngine.MeshCollider
struct MeshCollider_t60EB55ADE92499FE8D1AA206D2BD96E65B2766DE;
// UnityEngine.Sprite
struct Sprite_tCA09498D612D08DE668653AF1E9C12BF53434198;
// UnityEngine.UI.Selectable
struct Selectable_tAA9065030FE0468018DEC880302F95FEA9C0133A;
// UnityEngine.Windows.Speech.SemanticMeaning[]
struct SemanticMeaningU5BU5D_t3FC0A968EA1C540EEA6B6F92368A430CA596D23D;

struct SemanticMeaning_tF87995FD36CA45112E60A5F76AA211FA13351F0C_marshaled_com;
struct SemanticMeaning_tF87995FD36CA45112E60A5F76AA211FA13351F0C_marshaled_pinvoke;


IL2CPP_EXTERN_C_BEGIN
IL2CPP_EXTERN_C_END

#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif

// System.Object


// System.ValueType
struct  ValueType_t4D0C27076F7C36E76190FB3328E232BCB1CD1FFF  : public RuntimeObject
{
public:

public:
};

// Native definition for P/Invoke marshalling of System.ValueType
struct ValueType_t4D0C27076F7C36E76190FB3328E232BCB1CD1FFF_marshaled_pinvoke
{
};
// Native definition for COM marshalling of System.ValueType
struct ValueType_t4D0C27076F7C36E76190FB3328E232BCB1CD1FFF_marshaled_com
{
};

// Mirror.NetworkLobbyManager_PendingPlayer
struct  PendingPlayer_t40459CEDE375BC08A48A97694355F960BF84967F 
{
public:
	// Mirror.NetworkConnection Mirror.NetworkLobbyManager_PendingPlayer::conn
	NetworkConnection_tB7F48309DFDE730F2B8365840A48DFF388C8D553 * ___conn_0;
	// UnityEngine.GameObject Mirror.NetworkLobbyManager_PendingPlayer::lobbyPlayer
	GameObject_tBD1244AD56B4E59AAD76E5E7C9282EC5CE434F0F * ___lobbyPlayer_1;

public:
	inline static int32_t get_offset_of_conn_0() { return static_cast<int32_t>(offsetof(PendingPlayer_t40459CEDE375BC08A48A97694355F960BF84967F, ___conn_0)); }
	inline NetworkConnection_tB7F48309DFDE730F2B8365840A48DFF388C8D553 * get_conn_0() const { return ___conn_0; }
	inline NetworkConnection_tB7F48309DFDE730F2B8365840A48DFF388C8D553 ** get_address_of_conn_0() { return &___conn_0; }
	inline void set_conn_0(NetworkConnection_tB7F48309DFDE730F2B8365840A48DFF388C8D553 * value)
	{
		___conn_0 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___conn_0), (void*)value);
	}

	inline static int32_t get_offset_of_lobbyPlayer_1() { return static_cast<int32_t>(offsetof(PendingPlayer_t40459CEDE375BC08A48A97694355F960BF84967F, ___lobbyPlayer_1)); }
	inline GameObject_tBD1244AD56B4E59AAD76E5E7C9282EC5CE434F0F * get_lobbyPlayer_1() const { return ___lobbyPlayer_1; }
	inline GameObject_tBD1244AD56B4E59AAD76E5E7C9282EC5CE434F0F ** get_address_of_lobbyPlayer_1() { return &___lobbyPlayer_1; }
	inline void set_lobbyPlayer_1(GameObject_tBD1244AD56B4E59AAD76E5E7C9282EC5CE434F0F * value)
	{
		___lobbyPlayer_1 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___lobbyPlayer_1), (void*)value);
	}
};

// Native definition for P/Invoke marshalling of Mirror.NetworkLobbyManager/PendingPlayer
struct PendingPlayer_t40459CEDE375BC08A48A97694355F960BF84967F_marshaled_pinvoke
{
	NetworkConnection_tB7F48309DFDE730F2B8365840A48DFF388C8D553 * ___conn_0;
	GameObject_tBD1244AD56B4E59AAD76E5E7C9282EC5CE434F0F * ___lobbyPlayer_1;
};
// Native definition for COM marshalling of Mirror.NetworkLobbyManager/PendingPlayer
struct PendingPlayer_t40459CEDE375BC08A48A97694355F960BF84967F_marshaled_com
{
	NetworkConnection_tB7F48309DFDE730F2B8365840A48DFF388C8D553 * ___conn_0;
	GameObject_tBD1244AD56B4E59AAD76E5E7C9282EC5CE434F0F * ___lobbyPlayer_1;
};

// Mirror.NetworkMessage
struct  NetworkMessage_t7F8AC96151454587E6108E42F7EC0C2DD38A99BB 
{
public:
	// System.Int32 Mirror.NetworkMessage::msgType
	int32_t ___msgType_0;
	// Mirror.NetworkConnection Mirror.NetworkMessage::conn
	NetworkConnection_tB7F48309DFDE730F2B8365840A48DFF388C8D553 * ___conn_1;
	// Mirror.NetworkReader Mirror.NetworkMessage::reader
	NetworkReader_tAA88A75EFC73573BCCA5F501C814190B01ED4C25 * ___reader_2;

public:
	inline static int32_t get_offset_of_msgType_0() { return static_cast<int32_t>(offsetof(NetworkMessage_t7F8AC96151454587E6108E42F7EC0C2DD38A99BB, ___msgType_0)); }
	inline int32_t get_msgType_0() const { return ___msgType_0; }
	inline int32_t* get_address_of_msgType_0() { return &___msgType_0; }
	inline void set_msgType_0(int32_t value)
	{
		___msgType_0 = value;
	}

	inline static int32_t get_offset_of_conn_1() { return static_cast<int32_t>(offsetof(NetworkMessage_t7F8AC96151454587E6108E42F7EC0C2DD38A99BB, ___conn_1)); }
	inline NetworkConnection_tB7F48309DFDE730F2B8365840A48DFF388C8D553 * get_conn_1() const { return ___conn_1; }
	inline NetworkConnection_tB7F48309DFDE730F2B8365840A48DFF388C8D553 ** get_address_of_conn_1() { return &___conn_1; }
	inline void set_conn_1(NetworkConnection_tB7F48309DFDE730F2B8365840A48DFF388C8D553 * value)
	{
		___conn_1 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___conn_1), (void*)value);
	}

	inline static int32_t get_offset_of_reader_2() { return static_cast<int32_t>(offsetof(NetworkMessage_t7F8AC96151454587E6108E42F7EC0C2DD38A99BB, ___reader_2)); }
	inline NetworkReader_tAA88A75EFC73573BCCA5F501C814190B01ED4C25 * get_reader_2() const { return ___reader_2; }
	inline NetworkReader_tAA88A75EFC73573BCCA5F501C814190B01ED4C25 ** get_address_of_reader_2() { return &___reader_2; }
	inline void set_reader_2(NetworkReader_tAA88A75EFC73573BCCA5F501C814190B01ED4C25 * value)
	{
		___reader_2 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___reader_2), (void*)value);
	}
};

// Native definition for P/Invoke marshalling of Mirror.NetworkMessage
struct NetworkMessage_t7F8AC96151454587E6108E42F7EC0C2DD38A99BB_marshaled_pinvoke
{
	int32_t ___msgType_0;
	NetworkConnection_tB7F48309DFDE730F2B8365840A48DFF388C8D553 * ___conn_1;
	NetworkReader_tAA88A75EFC73573BCCA5F501C814190B01ED4C25 * ___reader_2;
};
// Native definition for COM marshalling of Mirror.NetworkMessage
struct NetworkMessage_t7F8AC96151454587E6108E42F7EC0C2DD38A99BB_marshaled_com
{
	int32_t ___msgType_0;
	NetworkConnection_tB7F48309DFDE730F2B8365840A48DFF388C8D553 * ___conn_1;
	NetworkReader_tAA88A75EFC73573BCCA5F501C814190B01ED4C25 * ___reader_2;
};

// Mono.Unity.UnityTls_unitytls_key_ref
struct  unitytls_key_ref_tE908606656A7C49CA1EB734722E4C3DED7CE6E5B 
{
public:
	// System.UInt64 Mono.Unity.UnityTls_unitytls_key_ref::handle
	uint64_t ___handle_0;

public:
	inline static int32_t get_offset_of_handle_0() { return static_cast<int32_t>(offsetof(unitytls_key_ref_tE908606656A7C49CA1EB734722E4C3DED7CE6E5B, ___handle_0)); }
	inline uint64_t get_handle_0() const { return ___handle_0; }
	inline uint64_t* get_address_of_handle_0() { return &___handle_0; }
	inline void set_handle_0(uint64_t value)
	{
		___handle_0 = value;
	}
};


// Mono.Unity.UnityTls_unitytls_tlsctx_callbacks
struct  unitytls_tlsctx_callbacks_t7BB5F622E014A8EC300C578657E2B0550DA828B2 
{
public:
	// Mono.Unity.UnityTls_unitytls_tlsctx_read_callback Mono.Unity.UnityTls_unitytls_tlsctx_callbacks::read
	unitytls_tlsctx_read_callback_tD85E7923018681355C1D851137CEC527F04093F5 * ___read_0;
	// Mono.Unity.UnityTls_unitytls_tlsctx_write_callback Mono.Unity.UnityTls_unitytls_tlsctx_callbacks::write
	unitytls_tlsctx_write_callback_tBDF40F27E011F577C3E834B14788491861F870D6 * ___write_1;
	// System.Void* Mono.Unity.UnityTls_unitytls_tlsctx_callbacks::data
	void* ___data_2;

public:
	inline static int32_t get_offset_of_read_0() { return static_cast<int32_t>(offsetof(unitytls_tlsctx_callbacks_t7BB5F622E014A8EC300C578657E2B0550DA828B2, ___read_0)); }
	inline unitytls_tlsctx_read_callback_tD85E7923018681355C1D851137CEC527F04093F5 * get_read_0() const { return ___read_0; }
	inline unitytls_tlsctx_read_callback_tD85E7923018681355C1D851137CEC527F04093F5 ** get_address_of_read_0() { return &___read_0; }
	inline void set_read_0(unitytls_tlsctx_read_callback_tD85E7923018681355C1D851137CEC527F04093F5 * value)
	{
		___read_0 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___read_0), (void*)value);
	}

	inline static int32_t get_offset_of_write_1() { return static_cast<int32_t>(offsetof(unitytls_tlsctx_callbacks_t7BB5F622E014A8EC300C578657E2B0550DA828B2, ___write_1)); }
	inline unitytls_tlsctx_write_callback_tBDF40F27E011F577C3E834B14788491861F870D6 * get_write_1() const { return ___write_1; }
	inline unitytls_tlsctx_write_callback_tBDF40F27E011F577C3E834B14788491861F870D6 ** get_address_of_write_1() { return &___write_1; }
	inline void set_write_1(unitytls_tlsctx_write_callback_tBDF40F27E011F577C3E834B14788491861F870D6 * value)
	{
		___write_1 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___write_1), (void*)value);
	}

	inline static int32_t get_offset_of_data_2() { return static_cast<int32_t>(offsetof(unitytls_tlsctx_callbacks_t7BB5F622E014A8EC300C578657E2B0550DA828B2, ___data_2)); }
	inline void* get_data_2() const { return ___data_2; }
	inline void** get_address_of_data_2() { return &___data_2; }
	inline void set_data_2(void* value)
	{
		___data_2 = value;
	}
};

// Native definition for P/Invoke marshalling of Mono.Unity.UnityTls/unitytls_tlsctx_callbacks
struct unitytls_tlsctx_callbacks_t7BB5F622E014A8EC300C578657E2B0550DA828B2_marshaled_pinvoke
{
	Il2CppMethodPointer ___read_0;
	Il2CppMethodPointer ___write_1;
	void* ___data_2;
};
// Native definition for COM marshalling of Mono.Unity.UnityTls/unitytls_tlsctx_callbacks
struct unitytls_tlsctx_callbacks_t7BB5F622E014A8EC300C578657E2B0550DA828B2_marshaled_com
{
	Il2CppMethodPointer ___read_0;
	Il2CppMethodPointer ___write_1;
	void* ___data_2;
};

// Mono.Unity.UnityTls_unitytls_x509_ref
struct  unitytls_x509_ref_tE1ED17887226610A1328A57FF787396C9457E7B7 
{
public:
	// System.UInt64 Mono.Unity.UnityTls_unitytls_x509_ref::handle
	uint64_t ___handle_0;

public:
	inline static int32_t get_offset_of_handle_0() { return static_cast<int32_t>(offsetof(unitytls_x509_ref_tE1ED17887226610A1328A57FF787396C9457E7B7, ___handle_0)); }
	inline uint64_t get_handle_0() const { return ___handle_0; }
	inline uint64_t* get_address_of_handle_0() { return &___handle_0; }
	inline void set_handle_0(uint64_t value)
	{
		___handle_0 = value;
	}
};


// Mono.Unity.UnityTls_unitytls_x509list_ref
struct  unitytls_x509list_ref_tF01A6BF5ADA9C454E6B975D2669AF22D27555BF6 
{
public:
	// System.UInt64 Mono.Unity.UnityTls_unitytls_x509list_ref::handle
	uint64_t ___handle_0;

public:
	inline static int32_t get_offset_of_handle_0() { return static_cast<int32_t>(offsetof(unitytls_x509list_ref_tF01A6BF5ADA9C454E6B975D2669AF22D27555BF6, ___handle_0)); }
	inline uint64_t get_handle_0() const { return ___handle_0; }
	inline uint64_t* get_address_of_handle_0() { return &___handle_0; }
	inline void set_handle_0(uint64_t value)
	{
		___handle_0 = value;
	}
};


// System.ArraySegment`1<System.Byte>
struct  ArraySegment_1_t5B17204266E698CC035E2A7F6435A4F78286D0FA 
{
public:
	// T[] System.ArraySegment`1::_array
	ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* ____array_0;
	// System.Int32 System.ArraySegment`1::_offset
	int32_t ____offset_1;
	// System.Int32 System.ArraySegment`1::_count
	int32_t ____count_2;

public:
	inline static int32_t get_offset_of__array_0() { return static_cast<int32_t>(offsetof(ArraySegment_1_t5B17204266E698CC035E2A7F6435A4F78286D0FA, ____array_0)); }
	inline ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* get__array_0() const { return ____array_0; }
	inline ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821** get_address_of__array_0() { return &____array_0; }
	inline void set__array_0(ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* value)
	{
		____array_0 = value;
		Il2CppCodeGenWriteBarrier((void**)(&____array_0), (void*)value);
	}

	inline static int32_t get_offset_of__offset_1() { return static_cast<int32_t>(offsetof(ArraySegment_1_t5B17204266E698CC035E2A7F6435A4F78286D0FA, ____offset_1)); }
	inline int32_t get__offset_1() const { return ____offset_1; }
	inline int32_t* get_address_of__offset_1() { return &____offset_1; }
	inline void set__offset_1(int32_t value)
	{
		____offset_1 = value;
	}

	inline static int32_t get_offset_of__count_2() { return static_cast<int32_t>(offsetof(ArraySegment_1_t5B17204266E698CC035E2A7F6435A4F78286D0FA, ____count_2)); }
	inline int32_t get__count_2() const { return ____count_2; }
	inline int32_t* get_address_of__count_2() { return &____count_2; }
	inline void set__count_2(int32_t value)
	{
		____count_2 = value;
	}
};


// System.Boolean
struct  Boolean_tB53F6830F670160873277339AA58F15CAED4399C 
{
public:
	// System.Boolean System.Boolean::m_value
	bool ___m_value_0;

public:
	inline static int32_t get_offset_of_m_value_0() { return static_cast<int32_t>(offsetof(Boolean_tB53F6830F670160873277339AA58F15CAED4399C, ___m_value_0)); }
	inline bool get_m_value_0() const { return ___m_value_0; }
	inline bool* get_address_of_m_value_0() { return &___m_value_0; }
	inline void set_m_value_0(bool value)
	{
		___m_value_0 = value;
	}
};

struct Boolean_tB53F6830F670160873277339AA58F15CAED4399C_StaticFields
{
public:
	// System.String System.Boolean::TrueString
	String_t* ___TrueString_5;
	// System.String System.Boolean::FalseString
	String_t* ___FalseString_6;

public:
	inline static int32_t get_offset_of_TrueString_5() { return static_cast<int32_t>(offsetof(Boolean_tB53F6830F670160873277339AA58F15CAED4399C_StaticFields, ___TrueString_5)); }
	inline String_t* get_TrueString_5() const { return ___TrueString_5; }
	inline String_t** get_address_of_TrueString_5() { return &___TrueString_5; }
	inline void set_TrueString_5(String_t* value)
	{
		___TrueString_5 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___TrueString_5), (void*)value);
	}

	inline static int32_t get_offset_of_FalseString_6() { return static_cast<int32_t>(offsetof(Boolean_tB53F6830F670160873277339AA58F15CAED4399C_StaticFields, ___FalseString_6)); }
	inline String_t* get_FalseString_6() const { return ___FalseString_6; }
	inline String_t** get_address_of_FalseString_6() { return &___FalseString_6; }
	inline void set_FalseString_6(String_t* value)
	{
		___FalseString_6 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___FalseString_6), (void*)value);
	}
};


// System.Collections.DictionaryEntry
struct  DictionaryEntry_tB5348A26B94274FCC1DD77185BD5946E283B11A4 
{
public:
	// System.Object System.Collections.DictionaryEntry::_key
	RuntimeObject * ____key_0;
	// System.Object System.Collections.DictionaryEntry::_value
	RuntimeObject * ____value_1;

public:
	inline static int32_t get_offset_of__key_0() { return static_cast<int32_t>(offsetof(DictionaryEntry_tB5348A26B94274FCC1DD77185BD5946E283B11A4, ____key_0)); }
	inline RuntimeObject * get__key_0() const { return ____key_0; }
	inline RuntimeObject ** get_address_of__key_0() { return &____key_0; }
	inline void set__key_0(RuntimeObject * value)
	{
		____key_0 = value;
		Il2CppCodeGenWriteBarrier((void**)(&____key_0), (void*)value);
	}

	inline static int32_t get_offset_of__value_1() { return static_cast<int32_t>(offsetof(DictionaryEntry_tB5348A26B94274FCC1DD77185BD5946E283B11A4, ____value_1)); }
	inline RuntimeObject * get__value_1() const { return ____value_1; }
	inline RuntimeObject ** get_address_of__value_1() { return &____value_1; }
	inline void set__value_1(RuntimeObject * value)
	{
		____value_1 = value;
		Il2CppCodeGenWriteBarrier((void**)(&____value_1), (void*)value);
	}
};

// Native definition for P/Invoke marshalling of System.Collections.DictionaryEntry
struct DictionaryEntry_tB5348A26B94274FCC1DD77185BD5946E283B11A4_marshaled_pinvoke
{
	Il2CppIUnknown* ____key_0;
	Il2CppIUnknown* ____value_1;
};
// Native definition for COM marshalling of System.Collections.DictionaryEntry
struct DictionaryEntry_tB5348A26B94274FCC1DD77185BD5946E283B11A4_marshaled_com
{
	Il2CppIUnknown* ____key_0;
	Il2CppIUnknown* ____value_1;
};

// System.Collections.Generic.KeyValuePair`2<System.Int32,System.Object>
struct  KeyValuePair_2_t86464C52F9602337EAC68825E6BE06951D7530CE 
{
public:
	// TKey System.Collections.Generic.KeyValuePair`2::key
	int32_t ___key_0;
	// TValue System.Collections.Generic.KeyValuePair`2::value
	RuntimeObject * ___value_1;

public:
	inline static int32_t get_offset_of_key_0() { return static_cast<int32_t>(offsetof(KeyValuePair_2_t86464C52F9602337EAC68825E6BE06951D7530CE, ___key_0)); }
	inline int32_t get_key_0() const { return ___key_0; }
	inline int32_t* get_address_of_key_0() { return &___key_0; }
	inline void set_key_0(int32_t value)
	{
		___key_0 = value;
	}

	inline static int32_t get_offset_of_value_1() { return static_cast<int32_t>(offsetof(KeyValuePair_2_t86464C52F9602337EAC68825E6BE06951D7530CE, ___value_1)); }
	inline RuntimeObject * get_value_1() const { return ___value_1; }
	inline RuntimeObject ** get_address_of_value_1() { return &___value_1; }
	inline void set_value_1(RuntimeObject * value)
	{
		___value_1 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___value_1), (void*)value);
	}
};


// System.Collections.Generic.KeyValuePair`2<System.Object,System.Object>
struct  KeyValuePair_2_t23481547E419E16E3B96A303578C1EB685C99EEE 
{
public:
	// TKey System.Collections.Generic.KeyValuePair`2::key
	RuntimeObject * ___key_0;
	// TValue System.Collections.Generic.KeyValuePair`2::value
	RuntimeObject * ___value_1;

public:
	inline static int32_t get_offset_of_key_0() { return static_cast<int32_t>(offsetof(KeyValuePair_2_t23481547E419E16E3B96A303578C1EB685C99EEE, ___key_0)); }
	inline RuntimeObject * get_key_0() const { return ___key_0; }
	inline RuntimeObject ** get_address_of_key_0() { return &___key_0; }
	inline void set_key_0(RuntimeObject * value)
	{
		___key_0 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___key_0), (void*)value);
	}

	inline static int32_t get_offset_of_value_1() { return static_cast<int32_t>(offsetof(KeyValuePair_2_t23481547E419E16E3B96A303578C1EB685C99EEE, ___value_1)); }
	inline RuntimeObject * get_value_1() const { return ___value_1; }
	inline RuntimeObject ** get_address_of_value_1() { return &___value_1; }
	inline void set_value_1(RuntimeObject * value)
	{
		___value_1 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___value_1), (void*)value);
	}
};


// System.DateTime
struct  DateTime_t349B7449FBAAFF4192636E2B7A07694DA9236132 
{
public:
	// System.UInt64 System.DateTime::dateData
	uint64_t ___dateData_44;

public:
	inline static int32_t get_offset_of_dateData_44() { return static_cast<int32_t>(offsetof(DateTime_t349B7449FBAAFF4192636E2B7A07694DA9236132, ___dateData_44)); }
	inline uint64_t get_dateData_44() const { return ___dateData_44; }
	inline uint64_t* get_address_of_dateData_44() { return &___dateData_44; }
	inline void set_dateData_44(uint64_t value)
	{
		___dateData_44 = value;
	}
};

struct DateTime_t349B7449FBAAFF4192636E2B7A07694DA9236132_StaticFields
{
public:
	// System.Int32[] System.DateTime::DaysToMonth365
	Int32U5BU5D_t2B9E4FDDDB9F0A00EC0AC631BA2DA915EB1ECF83* ___DaysToMonth365_29;
	// System.Int32[] System.DateTime::DaysToMonth366
	Int32U5BU5D_t2B9E4FDDDB9F0A00EC0AC631BA2DA915EB1ECF83* ___DaysToMonth366_30;
	// System.DateTime System.DateTime::MinValue
	DateTime_t349B7449FBAAFF4192636E2B7A07694DA9236132  ___MinValue_31;
	// System.DateTime System.DateTime::MaxValue
	DateTime_t349B7449FBAAFF4192636E2B7A07694DA9236132  ___MaxValue_32;

public:
	inline static int32_t get_offset_of_DaysToMonth365_29() { return static_cast<int32_t>(offsetof(DateTime_t349B7449FBAAFF4192636E2B7A07694DA9236132_StaticFields, ___DaysToMonth365_29)); }
	inline Int32U5BU5D_t2B9E4FDDDB9F0A00EC0AC631BA2DA915EB1ECF83* get_DaysToMonth365_29() const { return ___DaysToMonth365_29; }
	inline Int32U5BU5D_t2B9E4FDDDB9F0A00EC0AC631BA2DA915EB1ECF83** get_address_of_DaysToMonth365_29() { return &___DaysToMonth365_29; }
	inline void set_DaysToMonth365_29(Int32U5BU5D_t2B9E4FDDDB9F0A00EC0AC631BA2DA915EB1ECF83* value)
	{
		___DaysToMonth365_29 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___DaysToMonth365_29), (void*)value);
	}

	inline static int32_t get_offset_of_DaysToMonth366_30() { return static_cast<int32_t>(offsetof(DateTime_t349B7449FBAAFF4192636E2B7A07694DA9236132_StaticFields, ___DaysToMonth366_30)); }
	inline Int32U5BU5D_t2B9E4FDDDB9F0A00EC0AC631BA2DA915EB1ECF83* get_DaysToMonth366_30() const { return ___DaysToMonth366_30; }
	inline Int32U5BU5D_t2B9E4FDDDB9F0A00EC0AC631BA2DA915EB1ECF83** get_address_of_DaysToMonth366_30() { return &___DaysToMonth366_30; }
	inline void set_DaysToMonth366_30(Int32U5BU5D_t2B9E4FDDDB9F0A00EC0AC631BA2DA915EB1ECF83* value)
	{
		___DaysToMonth366_30 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___DaysToMonth366_30), (void*)value);
	}

	inline static int32_t get_offset_of_MinValue_31() { return static_cast<int32_t>(offsetof(DateTime_t349B7449FBAAFF4192636E2B7A07694DA9236132_StaticFields, ___MinValue_31)); }
	inline DateTime_t349B7449FBAAFF4192636E2B7A07694DA9236132  get_MinValue_31() const { return ___MinValue_31; }
	inline DateTime_t349B7449FBAAFF4192636E2B7A07694DA9236132 * get_address_of_MinValue_31() { return &___MinValue_31; }
	inline void set_MinValue_31(DateTime_t349B7449FBAAFF4192636E2B7A07694DA9236132  value)
	{
		___MinValue_31 = value;
	}

	inline static int32_t get_offset_of_MaxValue_32() { return static_cast<int32_t>(offsetof(DateTime_t349B7449FBAAFF4192636E2B7A07694DA9236132_StaticFields, ___MaxValue_32)); }
	inline DateTime_t349B7449FBAAFF4192636E2B7A07694DA9236132  get_MaxValue_32() const { return ___MaxValue_32; }
	inline DateTime_t349B7449FBAAFF4192636E2B7A07694DA9236132 * get_address_of_MaxValue_32() { return &___MaxValue_32; }
	inline void set_MaxValue_32(DateTime_t349B7449FBAAFF4192636E2B7A07694DA9236132  value)
	{
		___MaxValue_32 = value;
	}
};


// System.Decimal
struct  Decimal_t44EE9DA309A1BF848308DE4DDFC070CAE6D95EE8 
{
public:
	// System.Int32 System.Decimal::flags
	int32_t ___flags_14;
	// System.Int32 System.Decimal::hi
	int32_t ___hi_15;
	// System.Int32 System.Decimal::lo
	int32_t ___lo_16;
	// System.Int32 System.Decimal::mid
	int32_t ___mid_17;

public:
	inline static int32_t get_offset_of_flags_14() { return static_cast<int32_t>(offsetof(Decimal_t44EE9DA309A1BF848308DE4DDFC070CAE6D95EE8, ___flags_14)); }
	inline int32_t get_flags_14() const { return ___flags_14; }
	inline int32_t* get_address_of_flags_14() { return &___flags_14; }
	inline void set_flags_14(int32_t value)
	{
		___flags_14 = value;
	}

	inline static int32_t get_offset_of_hi_15() { return static_cast<int32_t>(offsetof(Decimal_t44EE9DA309A1BF848308DE4DDFC070CAE6D95EE8, ___hi_15)); }
	inline int32_t get_hi_15() const { return ___hi_15; }
	inline int32_t* get_address_of_hi_15() { return &___hi_15; }
	inline void set_hi_15(int32_t value)
	{
		___hi_15 = value;
	}

	inline static int32_t get_offset_of_lo_16() { return static_cast<int32_t>(offsetof(Decimal_t44EE9DA309A1BF848308DE4DDFC070CAE6D95EE8, ___lo_16)); }
	inline int32_t get_lo_16() const { return ___lo_16; }
	inline int32_t* get_address_of_lo_16() { return &___lo_16; }
	inline void set_lo_16(int32_t value)
	{
		___lo_16 = value;
	}

	inline static int32_t get_offset_of_mid_17() { return static_cast<int32_t>(offsetof(Decimal_t44EE9DA309A1BF848308DE4DDFC070CAE6D95EE8, ___mid_17)); }
	inline int32_t get_mid_17() const { return ___mid_17; }
	inline int32_t* get_address_of_mid_17() { return &___mid_17; }
	inline void set_mid_17(int32_t value)
	{
		___mid_17 = value;
	}
};

struct Decimal_t44EE9DA309A1BF848308DE4DDFC070CAE6D95EE8_StaticFields
{
public:
	// System.UInt32[] System.Decimal::Powers10
	UInt32U5BU5D_t9AA834AF2940E75BBF8E3F08FF0D20D266DB71CB* ___Powers10_6;
	// System.Decimal System.Decimal::Zero
	Decimal_t44EE9DA309A1BF848308DE4DDFC070CAE6D95EE8  ___Zero_7;
	// System.Decimal System.Decimal::One
	Decimal_t44EE9DA309A1BF848308DE4DDFC070CAE6D95EE8  ___One_8;
	// System.Decimal System.Decimal::MinusOne
	Decimal_t44EE9DA309A1BF848308DE4DDFC070CAE6D95EE8  ___MinusOne_9;
	// System.Decimal System.Decimal::MaxValue
	Decimal_t44EE9DA309A1BF848308DE4DDFC070CAE6D95EE8  ___MaxValue_10;
	// System.Decimal System.Decimal::MinValue
	Decimal_t44EE9DA309A1BF848308DE4DDFC070CAE6D95EE8  ___MinValue_11;
	// System.Decimal System.Decimal::NearNegativeZero
	Decimal_t44EE9DA309A1BF848308DE4DDFC070CAE6D95EE8  ___NearNegativeZero_12;
	// System.Decimal System.Decimal::NearPositiveZero
	Decimal_t44EE9DA309A1BF848308DE4DDFC070CAE6D95EE8  ___NearPositiveZero_13;

public:
	inline static int32_t get_offset_of_Powers10_6() { return static_cast<int32_t>(offsetof(Decimal_t44EE9DA309A1BF848308DE4DDFC070CAE6D95EE8_StaticFields, ___Powers10_6)); }
	inline UInt32U5BU5D_t9AA834AF2940E75BBF8E3F08FF0D20D266DB71CB* get_Powers10_6() const { return ___Powers10_6; }
	inline UInt32U5BU5D_t9AA834AF2940E75BBF8E3F08FF0D20D266DB71CB** get_address_of_Powers10_6() { return &___Powers10_6; }
	inline void set_Powers10_6(UInt32U5BU5D_t9AA834AF2940E75BBF8E3F08FF0D20D266DB71CB* value)
	{
		___Powers10_6 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___Powers10_6), (void*)value);
	}

	inline static int32_t get_offset_of_Zero_7() { return static_cast<int32_t>(offsetof(Decimal_t44EE9DA309A1BF848308DE4DDFC070CAE6D95EE8_StaticFields, ___Zero_7)); }
	inline Decimal_t44EE9DA309A1BF848308DE4DDFC070CAE6D95EE8  get_Zero_7() const { return ___Zero_7; }
	inline Decimal_t44EE9DA309A1BF848308DE4DDFC070CAE6D95EE8 * get_address_of_Zero_7() { return &___Zero_7; }
	inline void set_Zero_7(Decimal_t44EE9DA309A1BF848308DE4DDFC070CAE6D95EE8  value)
	{
		___Zero_7 = value;
	}

	inline static int32_t get_offset_of_One_8() { return static_cast<int32_t>(offsetof(Decimal_t44EE9DA309A1BF848308DE4DDFC070CAE6D95EE8_StaticFields, ___One_8)); }
	inline Decimal_t44EE9DA309A1BF848308DE4DDFC070CAE6D95EE8  get_One_8() const { return ___One_8; }
	inline Decimal_t44EE9DA309A1BF848308DE4DDFC070CAE6D95EE8 * get_address_of_One_8() { return &___One_8; }
	inline void set_One_8(Decimal_t44EE9DA309A1BF848308DE4DDFC070CAE6D95EE8  value)
	{
		___One_8 = value;
	}

	inline static int32_t get_offset_of_MinusOne_9() { return static_cast<int32_t>(offsetof(Decimal_t44EE9DA309A1BF848308DE4DDFC070CAE6D95EE8_StaticFields, ___MinusOne_9)); }
	inline Decimal_t44EE9DA309A1BF848308DE4DDFC070CAE6D95EE8  get_MinusOne_9() const { return ___MinusOne_9; }
	inline Decimal_t44EE9DA309A1BF848308DE4DDFC070CAE6D95EE8 * get_address_of_MinusOne_9() { return &___MinusOne_9; }
	inline void set_MinusOne_9(Decimal_t44EE9DA309A1BF848308DE4DDFC070CAE6D95EE8  value)
	{
		___MinusOne_9 = value;
	}

	inline static int32_t get_offset_of_MaxValue_10() { return static_cast<int32_t>(offsetof(Decimal_t44EE9DA309A1BF848308DE4DDFC070CAE6D95EE8_StaticFields, ___MaxValue_10)); }
	inline Decimal_t44EE9DA309A1BF848308DE4DDFC070CAE6D95EE8  get_MaxValue_10() const { return ___MaxValue_10; }
	inline Decimal_t44EE9DA309A1BF848308DE4DDFC070CAE6D95EE8 * get_address_of_MaxValue_10() { return &___MaxValue_10; }
	inline void set_MaxValue_10(Decimal_t44EE9DA309A1BF848308DE4DDFC070CAE6D95EE8  value)
	{
		___MaxValue_10 = value;
	}

	inline static int32_t get_offset_of_MinValue_11() { return static_cast<int32_t>(offsetof(Decimal_t44EE9DA309A1BF848308DE4DDFC070CAE6D95EE8_StaticFields, ___MinValue_11)); }
	inline Decimal_t44EE9DA309A1BF848308DE4DDFC070CAE6D95EE8  get_MinValue_11() const { return ___MinValue_11; }
	inline Decimal_t44EE9DA309A1BF848308DE4DDFC070CAE6D95EE8 * get_address_of_MinValue_11() { return &___MinValue_11; }
	inline void set_MinValue_11(Decimal_t44EE9DA309A1BF848308DE4DDFC070CAE6D95EE8  value)
	{
		___MinValue_11 = value;
	}

	inline static int32_t get_offset_of_NearNegativeZero_12() { return static_cast<int32_t>(offsetof(Decimal_t44EE9DA309A1BF848308DE4DDFC070CAE6D95EE8_StaticFields, ___NearNegativeZero_12)); }
	inline Decimal_t44EE9DA309A1BF848308DE4DDFC070CAE6D95EE8  get_NearNegativeZero_12() const { return ___NearNegativeZero_12; }
	inline Decimal_t44EE9DA309A1BF848308DE4DDFC070CAE6D95EE8 * get_address_of_NearNegativeZero_12() { return &___NearNegativeZero_12; }
	inline void set_NearNegativeZero_12(Decimal_t44EE9DA309A1BF848308DE4DDFC070CAE6D95EE8  value)
	{
		___NearNegativeZero_12 = value;
	}

	inline static int32_t get_offset_of_NearPositiveZero_13() { return static_cast<int32_t>(offsetof(Decimal_t44EE9DA309A1BF848308DE4DDFC070CAE6D95EE8_StaticFields, ___NearPositiveZero_13)); }
	inline Decimal_t44EE9DA309A1BF848308DE4DDFC070CAE6D95EE8  get_NearPositiveZero_13() const { return ___NearPositiveZero_13; }
	inline Decimal_t44EE9DA309A1BF848308DE4DDFC070CAE6D95EE8 * get_address_of_NearPositiveZero_13() { return &___NearPositiveZero_13; }
	inline void set_NearPositiveZero_13(Decimal_t44EE9DA309A1BF848308DE4DDFC070CAE6D95EE8  value)
	{
		___NearPositiveZero_13 = value;
	}
};


// System.Enum
struct  Enum_t2AF27C02B8653AE29442467390005ABC74D8F521  : public ValueType_t4D0C27076F7C36E76190FB3328E232BCB1CD1FFF
{
public:

public:
};

struct Enum_t2AF27C02B8653AE29442467390005ABC74D8F521_StaticFields
{
public:
	// System.Char[] System.Enum::enumSeperatorCharArray
	CharU5BU5D_t4CC6ABF0AD71BEC97E3C2F1E9C5677E46D3A75C2* ___enumSeperatorCharArray_0;

public:
	inline static int32_t get_offset_of_enumSeperatorCharArray_0() { return static_cast<int32_t>(offsetof(Enum_t2AF27C02B8653AE29442467390005ABC74D8F521_StaticFields, ___enumSeperatorCharArray_0)); }
	inline CharU5BU5D_t4CC6ABF0AD71BEC97E3C2F1E9C5677E46D3A75C2* get_enumSeperatorCharArray_0() const { return ___enumSeperatorCharArray_0; }
	inline CharU5BU5D_t4CC6ABF0AD71BEC97E3C2F1E9C5677E46D3A75C2** get_address_of_enumSeperatorCharArray_0() { return &___enumSeperatorCharArray_0; }
	inline void set_enumSeperatorCharArray_0(CharU5BU5D_t4CC6ABF0AD71BEC97E3C2F1E9C5677E46D3A75C2* value)
	{
		___enumSeperatorCharArray_0 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___enumSeperatorCharArray_0), (void*)value);
	}
};

// Native definition for P/Invoke marshalling of System.Enum
struct Enum_t2AF27C02B8653AE29442467390005ABC74D8F521_marshaled_pinvoke
{
};
// Native definition for COM marshalling of System.Enum
struct Enum_t2AF27C02B8653AE29442467390005ABC74D8F521_marshaled_com
{
};

// System.Guid
struct  Guid_t 
{
public:
	// System.Int32 System.Guid::_a
	int32_t ____a_1;
	// System.Int16 System.Guid::_b
	int16_t ____b_2;
	// System.Int16 System.Guid::_c
	int16_t ____c_3;
	// System.Byte System.Guid::_d
	uint8_t ____d_4;
	// System.Byte System.Guid::_e
	uint8_t ____e_5;
	// System.Byte System.Guid::_f
	uint8_t ____f_6;
	// System.Byte System.Guid::_g
	uint8_t ____g_7;
	// System.Byte System.Guid::_h
	uint8_t ____h_8;
	// System.Byte System.Guid::_i
	uint8_t ____i_9;
	// System.Byte System.Guid::_j
	uint8_t ____j_10;
	// System.Byte System.Guid::_k
	uint8_t ____k_11;

public:
	inline static int32_t get_offset_of__a_1() { return static_cast<int32_t>(offsetof(Guid_t, ____a_1)); }
	inline int32_t get__a_1() const { return ____a_1; }
	inline int32_t* get_address_of__a_1() { return &____a_1; }
	inline void set__a_1(int32_t value)
	{
		____a_1 = value;
	}

	inline static int32_t get_offset_of__b_2() { return static_cast<int32_t>(offsetof(Guid_t, ____b_2)); }
	inline int16_t get__b_2() const { return ____b_2; }
	inline int16_t* get_address_of__b_2() { return &____b_2; }
	inline void set__b_2(int16_t value)
	{
		____b_2 = value;
	}

	inline static int32_t get_offset_of__c_3() { return static_cast<int32_t>(offsetof(Guid_t, ____c_3)); }
	inline int16_t get__c_3() const { return ____c_3; }
	inline int16_t* get_address_of__c_3() { return &____c_3; }
	inline void set__c_3(int16_t value)
	{
		____c_3 = value;
	}

	inline static int32_t get_offset_of__d_4() { return static_cast<int32_t>(offsetof(Guid_t, ____d_4)); }
	inline uint8_t get__d_4() const { return ____d_4; }
	inline uint8_t* get_address_of__d_4() { return &____d_4; }
	inline void set__d_4(uint8_t value)
	{
		____d_4 = value;
	}

	inline static int32_t get_offset_of__e_5() { return static_cast<int32_t>(offsetof(Guid_t, ____e_5)); }
	inline uint8_t get__e_5() const { return ____e_5; }
	inline uint8_t* get_address_of__e_5() { return &____e_5; }
	inline void set__e_5(uint8_t value)
	{
		____e_5 = value;
	}

	inline static int32_t get_offset_of__f_6() { return static_cast<int32_t>(offsetof(Guid_t, ____f_6)); }
	inline uint8_t get__f_6() const { return ____f_6; }
	inline uint8_t* get_address_of__f_6() { return &____f_6; }
	inline void set__f_6(uint8_t value)
	{
		____f_6 = value;
	}

	inline static int32_t get_offset_of__g_7() { return static_cast<int32_t>(offsetof(Guid_t, ____g_7)); }
	inline uint8_t get__g_7() const { return ____g_7; }
	inline uint8_t* get_address_of__g_7() { return &____g_7; }
	inline void set__g_7(uint8_t value)
	{
		____g_7 = value;
	}

	inline static int32_t get_offset_of__h_8() { return static_cast<int32_t>(offsetof(Guid_t, ____h_8)); }
	inline uint8_t get__h_8() const { return ____h_8; }
	inline uint8_t* get_address_of__h_8() { return &____h_8; }
	inline void set__h_8(uint8_t value)
	{
		____h_8 = value;
	}

	inline static int32_t get_offset_of__i_9() { return static_cast<int32_t>(offsetof(Guid_t, ____i_9)); }
	inline uint8_t get__i_9() const { return ____i_9; }
	inline uint8_t* get_address_of__i_9() { return &____i_9; }
	inline void set__i_9(uint8_t value)
	{
		____i_9 = value;
	}

	inline static int32_t get_offset_of__j_10() { return static_cast<int32_t>(offsetof(Guid_t, ____j_10)); }
	inline uint8_t get__j_10() const { return ____j_10; }
	inline uint8_t* get_address_of__j_10() { return &____j_10; }
	inline void set__j_10(uint8_t value)
	{
		____j_10 = value;
	}

	inline static int32_t get_offset_of__k_11() { return static_cast<int32_t>(offsetof(Guid_t, ____k_11)); }
	inline uint8_t get__k_11() const { return ____k_11; }
	inline uint8_t* get_address_of__k_11() { return &____k_11; }
	inline void set__k_11(uint8_t value)
	{
		____k_11 = value;
	}
};

struct Guid_t_StaticFields
{
public:
	// System.Guid System.Guid::Empty
	Guid_t  ___Empty_0;
	// System.Object System.Guid::_rngAccess
	RuntimeObject * ____rngAccess_12;
	// System.Security.Cryptography.RandomNumberGenerator System.Guid::_rng
	RandomNumberGenerator_t12277F7F965BA79C54E4B3BFABD27A5FFB725EE2 * ____rng_13;
	// System.Security.Cryptography.RandomNumberGenerator System.Guid::_fastRng
	RandomNumberGenerator_t12277F7F965BA79C54E4B3BFABD27A5FFB725EE2 * ____fastRng_14;

public:
	inline static int32_t get_offset_of_Empty_0() { return static_cast<int32_t>(offsetof(Guid_t_StaticFields, ___Empty_0)); }
	inline Guid_t  get_Empty_0() const { return ___Empty_0; }
	inline Guid_t * get_address_of_Empty_0() { return &___Empty_0; }
	inline void set_Empty_0(Guid_t  value)
	{
		___Empty_0 = value;
	}

	inline static int32_t get_offset_of__rngAccess_12() { return static_cast<int32_t>(offsetof(Guid_t_StaticFields, ____rngAccess_12)); }
	inline RuntimeObject * get__rngAccess_12() const { return ____rngAccess_12; }
	inline RuntimeObject ** get_address_of__rngAccess_12() { return &____rngAccess_12; }
	inline void set__rngAccess_12(RuntimeObject * value)
	{
		____rngAccess_12 = value;
		Il2CppCodeGenWriteBarrier((void**)(&____rngAccess_12), (void*)value);
	}

	inline static int32_t get_offset_of__rng_13() { return static_cast<int32_t>(offsetof(Guid_t_StaticFields, ____rng_13)); }
	inline RandomNumberGenerator_t12277F7F965BA79C54E4B3BFABD27A5FFB725EE2 * get__rng_13() const { return ____rng_13; }
	inline RandomNumberGenerator_t12277F7F965BA79C54E4B3BFABD27A5FFB725EE2 ** get_address_of__rng_13() { return &____rng_13; }
	inline void set__rng_13(RandomNumberGenerator_t12277F7F965BA79C54E4B3BFABD27A5FFB725EE2 * value)
	{
		____rng_13 = value;
		Il2CppCodeGenWriteBarrier((void**)(&____rng_13), (void*)value);
	}

	inline static int32_t get_offset_of__fastRng_14() { return static_cast<int32_t>(offsetof(Guid_t_StaticFields, ____fastRng_14)); }
	inline RandomNumberGenerator_t12277F7F965BA79C54E4B3BFABD27A5FFB725EE2 * get__fastRng_14() const { return ____fastRng_14; }
	inline RandomNumberGenerator_t12277F7F965BA79C54E4B3BFABD27A5FFB725EE2 ** get_address_of__fastRng_14() { return &____fastRng_14; }
	inline void set__fastRng_14(RandomNumberGenerator_t12277F7F965BA79C54E4B3BFABD27A5FFB725EE2 * value)
	{
		____fastRng_14 = value;
		Il2CppCodeGenWriteBarrier((void**)(&____fastRng_14), (void*)value);
	}
};


// System.IO.Stream_ReadWriteParameters
struct  ReadWriteParameters_t5A9E416E0129249869039FC606326558DA3B597F 
{
public:
	// System.Byte[] System.IO.Stream_ReadWriteParameters::Buffer
	ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* ___Buffer_0;
	// System.Int32 System.IO.Stream_ReadWriteParameters::Offset
	int32_t ___Offset_1;
	// System.Int32 System.IO.Stream_ReadWriteParameters::Count
	int32_t ___Count_2;

public:
	inline static int32_t get_offset_of_Buffer_0() { return static_cast<int32_t>(offsetof(ReadWriteParameters_t5A9E416E0129249869039FC606326558DA3B597F, ___Buffer_0)); }
	inline ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* get_Buffer_0() const { return ___Buffer_0; }
	inline ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821** get_address_of_Buffer_0() { return &___Buffer_0; }
	inline void set_Buffer_0(ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* value)
	{
		___Buffer_0 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___Buffer_0), (void*)value);
	}

	inline static int32_t get_offset_of_Offset_1() { return static_cast<int32_t>(offsetof(ReadWriteParameters_t5A9E416E0129249869039FC606326558DA3B597F, ___Offset_1)); }
	inline int32_t get_Offset_1() const { return ___Offset_1; }
	inline int32_t* get_address_of_Offset_1() { return &___Offset_1; }
	inline void set_Offset_1(int32_t value)
	{
		___Offset_1 = value;
	}

	inline static int32_t get_offset_of_Count_2() { return static_cast<int32_t>(offsetof(ReadWriteParameters_t5A9E416E0129249869039FC606326558DA3B597F, ___Count_2)); }
	inline int32_t get_Count_2() const { return ___Count_2; }
	inline int32_t* get_address_of_Count_2() { return &___Count_2; }
	inline void set_Count_2(int32_t value)
	{
		___Count_2 = value;
	}
};

// Native definition for P/Invoke marshalling of System.IO.Stream/ReadWriteParameters
struct ReadWriteParameters_t5A9E416E0129249869039FC606326558DA3B597F_marshaled_pinvoke
{
	Il2CppSafeArray/*I1*/* ___Buffer_0;
	int32_t ___Offset_1;
	int32_t ___Count_2;
};
// Native definition for COM marshalling of System.IO.Stream/ReadWriteParameters
struct ReadWriteParameters_t5A9E416E0129249869039FC606326558DA3B597F_marshaled_com
{
	Il2CppSafeArray/*I1*/* ___Buffer_0;
	int32_t ___Offset_1;
	int32_t ___Count_2;
};

// System.IntPtr
struct  IntPtr_t 
{
public:
	// System.Void* System.IntPtr::m_value
	void* ___m_value_0;

public:
	inline static int32_t get_offset_of_m_value_0() { return static_cast<int32_t>(offsetof(IntPtr_t, ___m_value_0)); }
	inline void* get_m_value_0() const { return ___m_value_0; }
	inline void** get_address_of_m_value_0() { return &___m_value_0; }
	inline void set_m_value_0(void* value)
	{
		___m_value_0 = value;
	}
};

struct IntPtr_t_StaticFields
{
public:
	// System.IntPtr System.IntPtr::Zero
	intptr_t ___Zero_1;

public:
	inline static int32_t get_offset_of_Zero_1() { return static_cast<int32_t>(offsetof(IntPtr_t_StaticFields, ___Zero_1)); }
	inline intptr_t get_Zero_1() const { return ___Zero_1; }
	inline intptr_t* get_address_of_Zero_1() { return &___Zero_1; }
	inline void set_Zero_1(intptr_t value)
	{
		___Zero_1 = value;
	}
};


// System.Nullable`1<System.Int32>
struct  Nullable_1_t0D03270832B3FFDDC0E7C2D89D4A0EA25376A1EB 
{
public:
	// T System.Nullable`1::value
	int32_t ___value_0;
	// System.Boolean System.Nullable`1::has_value
	bool ___has_value_1;

public:
	inline static int32_t get_offset_of_value_0() { return static_cast<int32_t>(offsetof(Nullable_1_t0D03270832B3FFDDC0E7C2D89D4A0EA25376A1EB, ___value_0)); }
	inline int32_t get_value_0() const { return ___value_0; }
	inline int32_t* get_address_of_value_0() { return &___value_0; }
	inline void set_value_0(int32_t value)
	{
		___value_0 = value;
	}

	inline static int32_t get_offset_of_has_value_1() { return static_cast<int32_t>(offsetof(Nullable_1_t0D03270832B3FFDDC0E7C2D89D4A0EA25376A1EB, ___has_value_1)); }
	inline bool get_has_value_1() const { return ___has_value_1; }
	inline bool* get_address_of_has_value_1() { return &___has_value_1; }
	inline void set_has_value_1(bool value)
	{
		___has_value_1 = value;
	}
};


// System.Reflection.CustomAttributeTypedArgument
struct  CustomAttributeTypedArgument_t238ACCB3A438CB5EDE4A924C637B288CCEC958E8 
{
public:
	// System.Type System.Reflection.CustomAttributeTypedArgument::argumentType
	Type_t * ___argumentType_0;
	// System.Object System.Reflection.CustomAttributeTypedArgument::value
	RuntimeObject * ___value_1;

public:
	inline static int32_t get_offset_of_argumentType_0() { return static_cast<int32_t>(offsetof(CustomAttributeTypedArgument_t238ACCB3A438CB5EDE4A924C637B288CCEC958E8, ___argumentType_0)); }
	inline Type_t * get_argumentType_0() const { return ___argumentType_0; }
	inline Type_t ** get_address_of_argumentType_0() { return &___argumentType_0; }
	inline void set_argumentType_0(Type_t * value)
	{
		___argumentType_0 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___argumentType_0), (void*)value);
	}

	inline static int32_t get_offset_of_value_1() { return static_cast<int32_t>(offsetof(CustomAttributeTypedArgument_t238ACCB3A438CB5EDE4A924C637B288CCEC958E8, ___value_1)); }
	inline RuntimeObject * get_value_1() const { return ___value_1; }
	inline RuntimeObject ** get_address_of_value_1() { return &___value_1; }
	inline void set_value_1(RuntimeObject * value)
	{
		___value_1 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___value_1), (void*)value);
	}
};

// Native definition for P/Invoke marshalling of System.Reflection.CustomAttributeTypedArgument
struct CustomAttributeTypedArgument_t238ACCB3A438CB5EDE4A924C637B288CCEC958E8_marshaled_pinvoke
{
	Type_t * ___argumentType_0;
	Il2CppIUnknown* ___value_1;
};
// Native definition for COM marshalling of System.Reflection.CustomAttributeTypedArgument
struct CustomAttributeTypedArgument_t238ACCB3A438CB5EDE4A924C637B288CCEC958E8_marshaled_com
{
	Type_t * ___argumentType_0;
	Il2CppIUnknown* ___value_1;
};

// System.Security.Cryptography.DSAParameters
struct  DSAParameters_tCA1A96A151F47B1904693C457243E3B919425AC6 
{
public:
	// System.Byte[] System.Security.Cryptography.DSAParameters::P
	ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* ___P_0;
	// System.Byte[] System.Security.Cryptography.DSAParameters::Q
	ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* ___Q_1;
	// System.Byte[] System.Security.Cryptography.DSAParameters::G
	ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* ___G_2;
	// System.Byte[] System.Security.Cryptography.DSAParameters::Y
	ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* ___Y_3;
	// System.Byte[] System.Security.Cryptography.DSAParameters::J
	ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* ___J_4;
	// System.Byte[] System.Security.Cryptography.DSAParameters::X
	ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* ___X_5;
	// System.Byte[] System.Security.Cryptography.DSAParameters::Seed
	ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* ___Seed_6;
	// System.Int32 System.Security.Cryptography.DSAParameters::Counter
	int32_t ___Counter_7;

public:
	inline static int32_t get_offset_of_P_0() { return static_cast<int32_t>(offsetof(DSAParameters_tCA1A96A151F47B1904693C457243E3B919425AC6, ___P_0)); }
	inline ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* get_P_0() const { return ___P_0; }
	inline ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821** get_address_of_P_0() { return &___P_0; }
	inline void set_P_0(ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* value)
	{
		___P_0 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___P_0), (void*)value);
	}

	inline static int32_t get_offset_of_Q_1() { return static_cast<int32_t>(offsetof(DSAParameters_tCA1A96A151F47B1904693C457243E3B919425AC6, ___Q_1)); }
	inline ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* get_Q_1() const { return ___Q_1; }
	inline ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821** get_address_of_Q_1() { return &___Q_1; }
	inline void set_Q_1(ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* value)
	{
		___Q_1 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___Q_1), (void*)value);
	}

	inline static int32_t get_offset_of_G_2() { return static_cast<int32_t>(offsetof(DSAParameters_tCA1A96A151F47B1904693C457243E3B919425AC6, ___G_2)); }
	inline ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* get_G_2() const { return ___G_2; }
	inline ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821** get_address_of_G_2() { return &___G_2; }
	inline void set_G_2(ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* value)
	{
		___G_2 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___G_2), (void*)value);
	}

	inline static int32_t get_offset_of_Y_3() { return static_cast<int32_t>(offsetof(DSAParameters_tCA1A96A151F47B1904693C457243E3B919425AC6, ___Y_3)); }
	inline ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* get_Y_3() const { return ___Y_3; }
	inline ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821** get_address_of_Y_3() { return &___Y_3; }
	inline void set_Y_3(ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* value)
	{
		___Y_3 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___Y_3), (void*)value);
	}

	inline static int32_t get_offset_of_J_4() { return static_cast<int32_t>(offsetof(DSAParameters_tCA1A96A151F47B1904693C457243E3B919425AC6, ___J_4)); }
	inline ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* get_J_4() const { return ___J_4; }
	inline ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821** get_address_of_J_4() { return &___J_4; }
	inline void set_J_4(ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* value)
	{
		___J_4 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___J_4), (void*)value);
	}

	inline static int32_t get_offset_of_X_5() { return static_cast<int32_t>(offsetof(DSAParameters_tCA1A96A151F47B1904693C457243E3B919425AC6, ___X_5)); }
	inline ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* get_X_5() const { return ___X_5; }
	inline ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821** get_address_of_X_5() { return &___X_5; }
	inline void set_X_5(ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* value)
	{
		___X_5 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___X_5), (void*)value);
	}

	inline static int32_t get_offset_of_Seed_6() { return static_cast<int32_t>(offsetof(DSAParameters_tCA1A96A151F47B1904693C457243E3B919425AC6, ___Seed_6)); }
	inline ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* get_Seed_6() const { return ___Seed_6; }
	inline ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821** get_address_of_Seed_6() { return &___Seed_6; }
	inline void set_Seed_6(ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* value)
	{
		___Seed_6 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___Seed_6), (void*)value);
	}

	inline static int32_t get_offset_of_Counter_7() { return static_cast<int32_t>(offsetof(DSAParameters_tCA1A96A151F47B1904693C457243E3B919425AC6, ___Counter_7)); }
	inline int32_t get_Counter_7() const { return ___Counter_7; }
	inline int32_t* get_address_of_Counter_7() { return &___Counter_7; }
	inline void set_Counter_7(int32_t value)
	{
		___Counter_7 = value;
	}
};

// Native definition for P/Invoke marshalling of System.Security.Cryptography.DSAParameters
struct DSAParameters_tCA1A96A151F47B1904693C457243E3B919425AC6_marshaled_pinvoke
{
	Il2CppSafeArray/*I1*/* ___P_0;
	Il2CppSafeArray/*I1*/* ___Q_1;
	Il2CppSafeArray/*I1*/* ___G_2;
	Il2CppSafeArray/*I1*/* ___Y_3;
	Il2CppSafeArray/*I1*/* ___J_4;
	Il2CppSafeArray/*I1*/* ___X_5;
	Il2CppSafeArray/*I1*/* ___Seed_6;
	int32_t ___Counter_7;
};
// Native definition for COM marshalling of System.Security.Cryptography.DSAParameters
struct DSAParameters_tCA1A96A151F47B1904693C457243E3B919425AC6_marshaled_com
{
	Il2CppSafeArray/*I1*/* ___P_0;
	Il2CppSafeArray/*I1*/* ___Q_1;
	Il2CppSafeArray/*I1*/* ___G_2;
	Il2CppSafeArray/*I1*/* ___Y_3;
	Il2CppSafeArray/*I1*/* ___J_4;
	Il2CppSafeArray/*I1*/* ___X_5;
	Il2CppSafeArray/*I1*/* ___Seed_6;
	int32_t ___Counter_7;
};

// System.Security.Cryptography.RSAParameters
struct  RSAParameters_t6A568C1275FA8F8C02615666D998134DCFFB9717 
{
public:
	// System.Byte[] System.Security.Cryptography.RSAParameters::Exponent
	ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* ___Exponent_0;
	// System.Byte[] System.Security.Cryptography.RSAParameters::Modulus
	ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* ___Modulus_1;
	// System.Byte[] System.Security.Cryptography.RSAParameters::P
	ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* ___P_2;
	// System.Byte[] System.Security.Cryptography.RSAParameters::Q
	ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* ___Q_3;
	// System.Byte[] System.Security.Cryptography.RSAParameters::DP
	ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* ___DP_4;
	// System.Byte[] System.Security.Cryptography.RSAParameters::DQ
	ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* ___DQ_5;
	// System.Byte[] System.Security.Cryptography.RSAParameters::InverseQ
	ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* ___InverseQ_6;
	// System.Byte[] System.Security.Cryptography.RSAParameters::D
	ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* ___D_7;

public:
	inline static int32_t get_offset_of_Exponent_0() { return static_cast<int32_t>(offsetof(RSAParameters_t6A568C1275FA8F8C02615666D998134DCFFB9717, ___Exponent_0)); }
	inline ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* get_Exponent_0() const { return ___Exponent_0; }
	inline ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821** get_address_of_Exponent_0() { return &___Exponent_0; }
	inline void set_Exponent_0(ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* value)
	{
		___Exponent_0 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___Exponent_0), (void*)value);
	}

	inline static int32_t get_offset_of_Modulus_1() { return static_cast<int32_t>(offsetof(RSAParameters_t6A568C1275FA8F8C02615666D998134DCFFB9717, ___Modulus_1)); }
	inline ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* get_Modulus_1() const { return ___Modulus_1; }
	inline ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821** get_address_of_Modulus_1() { return &___Modulus_1; }
	inline void set_Modulus_1(ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* value)
	{
		___Modulus_1 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___Modulus_1), (void*)value);
	}

	inline static int32_t get_offset_of_P_2() { return static_cast<int32_t>(offsetof(RSAParameters_t6A568C1275FA8F8C02615666D998134DCFFB9717, ___P_2)); }
	inline ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* get_P_2() const { return ___P_2; }
	inline ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821** get_address_of_P_2() { return &___P_2; }
	inline void set_P_2(ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* value)
	{
		___P_2 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___P_2), (void*)value);
	}

	inline static int32_t get_offset_of_Q_3() { return static_cast<int32_t>(offsetof(RSAParameters_t6A568C1275FA8F8C02615666D998134DCFFB9717, ___Q_3)); }
	inline ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* get_Q_3() const { return ___Q_3; }
	inline ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821** get_address_of_Q_3() { return &___Q_3; }
	inline void set_Q_3(ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* value)
	{
		___Q_3 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___Q_3), (void*)value);
	}

	inline static int32_t get_offset_of_DP_4() { return static_cast<int32_t>(offsetof(RSAParameters_t6A568C1275FA8F8C02615666D998134DCFFB9717, ___DP_4)); }
	inline ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* get_DP_4() const { return ___DP_4; }
	inline ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821** get_address_of_DP_4() { return &___DP_4; }
	inline void set_DP_4(ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* value)
	{
		___DP_4 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___DP_4), (void*)value);
	}

	inline static int32_t get_offset_of_DQ_5() { return static_cast<int32_t>(offsetof(RSAParameters_t6A568C1275FA8F8C02615666D998134DCFFB9717, ___DQ_5)); }
	inline ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* get_DQ_5() const { return ___DQ_5; }
	inline ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821** get_address_of_DQ_5() { return &___DQ_5; }
	inline void set_DQ_5(ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* value)
	{
		___DQ_5 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___DQ_5), (void*)value);
	}

	inline static int32_t get_offset_of_InverseQ_6() { return static_cast<int32_t>(offsetof(RSAParameters_t6A568C1275FA8F8C02615666D998134DCFFB9717, ___InverseQ_6)); }
	inline ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* get_InverseQ_6() const { return ___InverseQ_6; }
	inline ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821** get_address_of_InverseQ_6() { return &___InverseQ_6; }
	inline void set_InverseQ_6(ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* value)
	{
		___InverseQ_6 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___InverseQ_6), (void*)value);
	}

	inline static int32_t get_offset_of_D_7() { return static_cast<int32_t>(offsetof(RSAParameters_t6A568C1275FA8F8C02615666D998134DCFFB9717, ___D_7)); }
	inline ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* get_D_7() const { return ___D_7; }
	inline ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821** get_address_of_D_7() { return &___D_7; }
	inline void set_D_7(ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* value)
	{
		___D_7 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___D_7), (void*)value);
	}
};

// Native definition for P/Invoke marshalling of System.Security.Cryptography.RSAParameters
struct RSAParameters_t6A568C1275FA8F8C02615666D998134DCFFB9717_marshaled_pinvoke
{
	Il2CppSafeArray/*I1*/* ___Exponent_0;
	Il2CppSafeArray/*I1*/* ___Modulus_1;
	Il2CppSafeArray/*I1*/* ___P_2;
	Il2CppSafeArray/*I1*/* ___Q_3;
	Il2CppSafeArray/*I1*/* ___DP_4;
	Il2CppSafeArray/*I1*/* ___DQ_5;
	Il2CppSafeArray/*I1*/* ___InverseQ_6;
	Il2CppSafeArray/*I1*/* ___D_7;
};
// Native definition for COM marshalling of System.Security.Cryptography.RSAParameters
struct RSAParameters_t6A568C1275FA8F8C02615666D998134DCFFB9717_marshaled_com
{
	Il2CppSafeArray/*I1*/* ___Exponent_0;
	Il2CppSafeArray/*I1*/* ___Modulus_1;
	Il2CppSafeArray/*I1*/* ___P_2;
	Il2CppSafeArray/*I1*/* ___Q_3;
	Il2CppSafeArray/*I1*/* ___DP_4;
	Il2CppSafeArray/*I1*/* ___DQ_5;
	Il2CppSafeArray/*I1*/* ___InverseQ_6;
	Il2CppSafeArray/*I1*/* ___D_7;
};

// System.Threading.AsyncLocalValueChangedArgs`1<System.Object>
struct  AsyncLocalValueChangedArgs_1_t64BF6800935406CA808E9821DF12DBB72A71640D 
{
public:
	// T System.Threading.AsyncLocalValueChangedArgs`1::<PreviousValue>k__BackingField
	RuntimeObject * ___U3CPreviousValueU3Ek__BackingField_0;
	// T System.Threading.AsyncLocalValueChangedArgs`1::<CurrentValue>k__BackingField
	RuntimeObject * ___U3CCurrentValueU3Ek__BackingField_1;
	// System.Boolean System.Threading.AsyncLocalValueChangedArgs`1::<ThreadContextChanged>k__BackingField
	bool ___U3CThreadContextChangedU3Ek__BackingField_2;

public:
	inline static int32_t get_offset_of_U3CPreviousValueU3Ek__BackingField_0() { return static_cast<int32_t>(offsetof(AsyncLocalValueChangedArgs_1_t64BF6800935406CA808E9821DF12DBB72A71640D, ___U3CPreviousValueU3Ek__BackingField_0)); }
	inline RuntimeObject * get_U3CPreviousValueU3Ek__BackingField_0() const { return ___U3CPreviousValueU3Ek__BackingField_0; }
	inline RuntimeObject ** get_address_of_U3CPreviousValueU3Ek__BackingField_0() { return &___U3CPreviousValueU3Ek__BackingField_0; }
	inline void set_U3CPreviousValueU3Ek__BackingField_0(RuntimeObject * value)
	{
		___U3CPreviousValueU3Ek__BackingField_0 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___U3CPreviousValueU3Ek__BackingField_0), (void*)value);
	}

	inline static int32_t get_offset_of_U3CCurrentValueU3Ek__BackingField_1() { return static_cast<int32_t>(offsetof(AsyncLocalValueChangedArgs_1_t64BF6800935406CA808E9821DF12DBB72A71640D, ___U3CCurrentValueU3Ek__BackingField_1)); }
	inline RuntimeObject * get_U3CCurrentValueU3Ek__BackingField_1() const { return ___U3CCurrentValueU3Ek__BackingField_1; }
	inline RuntimeObject ** get_address_of_U3CCurrentValueU3Ek__BackingField_1() { return &___U3CCurrentValueU3Ek__BackingField_1; }
	inline void set_U3CCurrentValueU3Ek__BackingField_1(RuntimeObject * value)
	{
		___U3CCurrentValueU3Ek__BackingField_1 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___U3CCurrentValueU3Ek__BackingField_1), (void*)value);
	}

	inline static int32_t get_offset_of_U3CThreadContextChangedU3Ek__BackingField_2() { return static_cast<int32_t>(offsetof(AsyncLocalValueChangedArgs_1_t64BF6800935406CA808E9821DF12DBB72A71640D, ___U3CThreadContextChangedU3Ek__BackingField_2)); }
	inline bool get_U3CThreadContextChangedU3Ek__BackingField_2() const { return ___U3CThreadContextChangedU3Ek__BackingField_2; }
	inline bool* get_address_of_U3CThreadContextChangedU3Ek__BackingField_2() { return &___U3CThreadContextChangedU3Ek__BackingField_2; }
	inline void set_U3CThreadContextChangedU3Ek__BackingField_2(bool value)
	{
		___U3CThreadContextChangedU3Ek__BackingField_2 = value;
	}
};


// System.Threading.CancellationToken
struct  CancellationToken_t9E956952F7F20908F2AE72EDF36D97E6C7DB63AB 
{
public:
	// System.Threading.CancellationTokenSource System.Threading.CancellationToken::m_source
	CancellationTokenSource_tF480B7E74A032667AFBD31F0530D619FB43AD3FE * ___m_source_0;

public:
	inline static int32_t get_offset_of_m_source_0() { return static_cast<int32_t>(offsetof(CancellationToken_t9E956952F7F20908F2AE72EDF36D97E6C7DB63AB, ___m_source_0)); }
	inline CancellationTokenSource_tF480B7E74A032667AFBD31F0530D619FB43AD3FE * get_m_source_0() const { return ___m_source_0; }
	inline CancellationTokenSource_tF480B7E74A032667AFBD31F0530D619FB43AD3FE ** get_address_of_m_source_0() { return &___m_source_0; }
	inline void set_m_source_0(CancellationTokenSource_tF480B7E74A032667AFBD31F0530D619FB43AD3FE * value)
	{
		___m_source_0 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___m_source_0), (void*)value);
	}
};

struct CancellationToken_t9E956952F7F20908F2AE72EDF36D97E6C7DB63AB_StaticFields
{
public:
	// System.Action`1<System.Object> System.Threading.CancellationToken::s_ActionToActionObjShunt
	Action_1_t551A279CEADCF6EEAE8FA2B1E1E757D0D15290D0 * ___s_ActionToActionObjShunt_1;

public:
	inline static int32_t get_offset_of_s_ActionToActionObjShunt_1() { return static_cast<int32_t>(offsetof(CancellationToken_t9E956952F7F20908F2AE72EDF36D97E6C7DB63AB_StaticFields, ___s_ActionToActionObjShunt_1)); }
	inline Action_1_t551A279CEADCF6EEAE8FA2B1E1E757D0D15290D0 * get_s_ActionToActionObjShunt_1() const { return ___s_ActionToActionObjShunt_1; }
	inline Action_1_t551A279CEADCF6EEAE8FA2B1E1E757D0D15290D0 ** get_address_of_s_ActionToActionObjShunt_1() { return &___s_ActionToActionObjShunt_1; }
	inline void set_s_ActionToActionObjShunt_1(Action_1_t551A279CEADCF6EEAE8FA2B1E1E757D0D15290D0 * value)
	{
		___s_ActionToActionObjShunt_1 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___s_ActionToActionObjShunt_1), (void*)value);
	}
};

// Native definition for P/Invoke marshalling of System.Threading.CancellationToken
struct CancellationToken_t9E956952F7F20908F2AE72EDF36D97E6C7DB63AB_marshaled_pinvoke
{
	CancellationTokenSource_tF480B7E74A032667AFBD31F0530D619FB43AD3FE * ___m_source_0;
};
// Native definition for COM marshalling of System.Threading.CancellationToken
struct CancellationToken_t9E956952F7F20908F2AE72EDF36D97E6C7DB63AB_marshaled_com
{
	CancellationTokenSource_tF480B7E74A032667AFBD31F0530D619FB43AD3FE * ___m_source_0;
};

// System.Threading.Tasks.VoidTaskResult
struct  VoidTaskResult_t66EBC10DDE738848DB00F6EC1A2536D7D4715F40 
{
public:
	union
	{
		struct
		{
		};
		uint8_t VoidTaskResult_t66EBC10DDE738848DB00F6EC1A2536D7D4715F40__padding[1];
	};

public:
};


// System.ValueTuple`2<System.Int32,System.Boolean>
struct  ValueTuple_2_t446756A8057B54D18CAD5BA1D73699DA4B40A264 
{
public:
	// T1 System.ValueTuple`2::Item1
	int32_t ___Item1_0;
	// T2 System.ValueTuple`2::Item2
	bool ___Item2_1;

public:
	inline static int32_t get_offset_of_Item1_0() { return static_cast<int32_t>(offsetof(ValueTuple_2_t446756A8057B54D18CAD5BA1D73699DA4B40A264, ___Item1_0)); }
	inline int32_t get_Item1_0() const { return ___Item1_0; }
	inline int32_t* get_address_of_Item1_0() { return &___Item1_0; }
	inline void set_Item1_0(int32_t value)
	{
		___Item1_0 = value;
	}

	inline static int32_t get_offset_of_Item2_1() { return static_cast<int32_t>(offsetof(ValueTuple_2_t446756A8057B54D18CAD5BA1D73699DA4B40A264, ___Item2_1)); }
	inline bool get_Item2_1() const { return ___Item2_1; }
	inline bool* get_address_of_Item2_1() { return &___Item2_1; }
	inline void set_Item2_1(bool value)
	{
		___Item2_1 = value;
	}
};


// UnityEngine.BeforeRenderHelper_OrderBlock
struct  OrderBlock_t3B2BBCE8320FAEC3DB605F7DC9AB641102F53727 
{
public:
	// System.Int32 UnityEngine.BeforeRenderHelper_OrderBlock::order
	int32_t ___order_0;
	// UnityEngine.Events.UnityAction UnityEngine.BeforeRenderHelper_OrderBlock::callback
	UnityAction_tD19B26F1B2C048E38FD5801A33573BE01064CAF4 * ___callback_1;

public:
	inline static int32_t get_offset_of_order_0() { return static_cast<int32_t>(offsetof(OrderBlock_t3B2BBCE8320FAEC3DB605F7DC9AB641102F53727, ___order_0)); }
	inline int32_t get_order_0() const { return ___order_0; }
	inline int32_t* get_address_of_order_0() { return &___order_0; }
	inline void set_order_0(int32_t value)
	{
		___order_0 = value;
	}

	inline static int32_t get_offset_of_callback_1() { return static_cast<int32_t>(offsetof(OrderBlock_t3B2BBCE8320FAEC3DB605F7DC9AB641102F53727, ___callback_1)); }
	inline UnityAction_tD19B26F1B2C048E38FD5801A33573BE01064CAF4 * get_callback_1() const { return ___callback_1; }
	inline UnityAction_tD19B26F1B2C048E38FD5801A33573BE01064CAF4 ** get_address_of_callback_1() { return &___callback_1; }
	inline void set_callback_1(UnityAction_tD19B26F1B2C048E38FD5801A33573BE01064CAF4 * value)
	{
		___callback_1 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___callback_1), (void*)value);
	}
};

// Native definition for P/Invoke marshalling of UnityEngine.BeforeRenderHelper/OrderBlock
struct OrderBlock_t3B2BBCE8320FAEC3DB605F7DC9AB641102F53727_marshaled_pinvoke
{
	int32_t ___order_0;
	Il2CppMethodPointer ___callback_1;
};
// Native definition for COM marshalling of UnityEngine.BeforeRenderHelper/OrderBlock
struct OrderBlock_t3B2BBCE8320FAEC3DB605F7DC9AB641102F53727_marshaled_com
{
	int32_t ___order_0;
	Il2CppMethodPointer ___callback_1;
};

// UnityEngine.Color
struct  Color_t119BCA590009762C7223FDD3AF9706653AC84ED2 
{
public:
	// System.Single UnityEngine.Color::r
	float ___r_0;
	// System.Single UnityEngine.Color::g
	float ___g_1;
	// System.Single UnityEngine.Color::b
	float ___b_2;
	// System.Single UnityEngine.Color::a
	float ___a_3;

public:
	inline static int32_t get_offset_of_r_0() { return static_cast<int32_t>(offsetof(Color_t119BCA590009762C7223FDD3AF9706653AC84ED2, ___r_0)); }
	inline float get_r_0() const { return ___r_0; }
	inline float* get_address_of_r_0() { return &___r_0; }
	inline void set_r_0(float value)
	{
		___r_0 = value;
	}

	inline static int32_t get_offset_of_g_1() { return static_cast<int32_t>(offsetof(Color_t119BCA590009762C7223FDD3AF9706653AC84ED2, ___g_1)); }
	inline float get_g_1() const { return ___g_1; }
	inline float* get_address_of_g_1() { return &___g_1; }
	inline void set_g_1(float value)
	{
		___g_1 = value;
	}

	inline static int32_t get_offset_of_b_2() { return static_cast<int32_t>(offsetof(Color_t119BCA590009762C7223FDD3AF9706653AC84ED2, ___b_2)); }
	inline float get_b_2() const { return ___b_2; }
	inline float* get_address_of_b_2() { return &___b_2; }
	inline void set_b_2(float value)
	{
		___b_2 = value;
	}

	inline static int32_t get_offset_of_a_3() { return static_cast<int32_t>(offsetof(Color_t119BCA590009762C7223FDD3AF9706653AC84ED2, ___a_3)); }
	inline float get_a_3() const { return ___a_3; }
	inline float* get_address_of_a_3() { return &___a_3; }
	inline void set_a_3(float value)
	{
		___a_3 = value;
	}
};


// UnityEngine.Color32
struct  Color32_t23ABC4AE0E0BDFD2E22EE1FA0DA3904FFE5F6E23 
{
public:
	union
	{
		#pragma pack(push, tp, 1)
		struct
		{
			// System.Int32 UnityEngine.Color32::rgba
			int32_t ___rgba_0;
		};
		#pragma pack(pop, tp)
		struct
		{
			int32_t ___rgba_0_forAlignmentOnly;
		};
		#pragma pack(push, tp, 1)
		struct
		{
			// System.Byte UnityEngine.Color32::r
			uint8_t ___r_1;
		};
		#pragma pack(pop, tp)
		struct
		{
			uint8_t ___r_1_forAlignmentOnly;
		};
		#pragma pack(push, tp, 1)
		struct
		{
			char ___g_2_OffsetPadding[1];
			// System.Byte UnityEngine.Color32::g
			uint8_t ___g_2;
		};
		#pragma pack(pop, tp)
		struct
		{
			char ___g_2_OffsetPadding_forAlignmentOnly[1];
			uint8_t ___g_2_forAlignmentOnly;
		};
		#pragma pack(push, tp, 1)
		struct
		{
			char ___b_3_OffsetPadding[2];
			// System.Byte UnityEngine.Color32::b
			uint8_t ___b_3;
		};
		#pragma pack(pop, tp)
		struct
		{
			char ___b_3_OffsetPadding_forAlignmentOnly[2];
			uint8_t ___b_3_forAlignmentOnly;
		};
		#pragma pack(push, tp, 1)
		struct
		{
			char ___a_4_OffsetPadding[3];
			// System.Byte UnityEngine.Color32::a
			uint8_t ___a_4;
		};
		#pragma pack(pop, tp)
		struct
		{
			char ___a_4_OffsetPadding_forAlignmentOnly[3];
			uint8_t ___a_4_forAlignmentOnly;
		};
	};

public:
	inline static int32_t get_offset_of_rgba_0() { return static_cast<int32_t>(offsetof(Color32_t23ABC4AE0E0BDFD2E22EE1FA0DA3904FFE5F6E23, ___rgba_0)); }
	inline int32_t get_rgba_0() const { return ___rgba_0; }
	inline int32_t* get_address_of_rgba_0() { return &___rgba_0; }
	inline void set_rgba_0(int32_t value)
	{
		___rgba_0 = value;
	}

	inline static int32_t get_offset_of_r_1() { return static_cast<int32_t>(offsetof(Color32_t23ABC4AE0E0BDFD2E22EE1FA0DA3904FFE5F6E23, ___r_1)); }
	inline uint8_t get_r_1() const { return ___r_1; }
	inline uint8_t* get_address_of_r_1() { return &___r_1; }
	inline void set_r_1(uint8_t value)
	{
		___r_1 = value;
	}

	inline static int32_t get_offset_of_g_2() { return static_cast<int32_t>(offsetof(Color32_t23ABC4AE0E0BDFD2E22EE1FA0DA3904FFE5F6E23, ___g_2)); }
	inline uint8_t get_g_2() const { return ___g_2; }
	inline uint8_t* get_address_of_g_2() { return &___g_2; }
	inline void set_g_2(uint8_t value)
	{
		___g_2 = value;
	}

	inline static int32_t get_offset_of_b_3() { return static_cast<int32_t>(offsetof(Color32_t23ABC4AE0E0BDFD2E22EE1FA0DA3904FFE5F6E23, ___b_3)); }
	inline uint8_t get_b_3() const { return ___b_3; }
	inline uint8_t* get_address_of_b_3() { return &___b_3; }
	inline void set_b_3(uint8_t value)
	{
		___b_3 = value;
	}

	inline static int32_t get_offset_of_a_4() { return static_cast<int32_t>(offsetof(Color32_t23ABC4AE0E0BDFD2E22EE1FA0DA3904FFE5F6E23, ___a_4)); }
	inline uint8_t get_a_4() const { return ___a_4; }
	inline uint8_t* get_address_of_a_4() { return &___a_4; }
	inline void set_a_4(uint8_t value)
	{
		___a_4 = value;
	}
};


// UnityEngine.CullingGroupEvent
struct  CullingGroupEvent_tC36FFE61D0A4E7B31F575A1FCAEE05AC41FACA85 
{
public:
	// System.Int32 UnityEngine.CullingGroupEvent::m_Index
	int32_t ___m_Index_0;
	// System.Byte UnityEngine.CullingGroupEvent::m_PrevState
	uint8_t ___m_PrevState_1;
	// System.Byte UnityEngine.CullingGroupEvent::m_ThisState
	uint8_t ___m_ThisState_2;

public:
	inline static int32_t get_offset_of_m_Index_0() { return static_cast<int32_t>(offsetof(CullingGroupEvent_tC36FFE61D0A4E7B31F575A1FCAEE05AC41FACA85, ___m_Index_0)); }
	inline int32_t get_m_Index_0() const { return ___m_Index_0; }
	inline int32_t* get_address_of_m_Index_0() { return &___m_Index_0; }
	inline void set_m_Index_0(int32_t value)
	{
		___m_Index_0 = value;
	}

	inline static int32_t get_offset_of_m_PrevState_1() { return static_cast<int32_t>(offsetof(CullingGroupEvent_tC36FFE61D0A4E7B31F575A1FCAEE05AC41FACA85, ___m_PrevState_1)); }
	inline uint8_t get_m_PrevState_1() const { return ___m_PrevState_1; }
	inline uint8_t* get_address_of_m_PrevState_1() { return &___m_PrevState_1; }
	inline void set_m_PrevState_1(uint8_t value)
	{
		___m_PrevState_1 = value;
	}

	inline static int32_t get_offset_of_m_ThisState_2() { return static_cast<int32_t>(offsetof(CullingGroupEvent_tC36FFE61D0A4E7B31F575A1FCAEE05AC41FACA85, ___m_ThisState_2)); }
	inline uint8_t get_m_ThisState_2() const { return ___m_ThisState_2; }
	inline uint8_t* get_address_of_m_ThisState_2() { return &___m_ThisState_2; }
	inline void set_m_ThisState_2(uint8_t value)
	{
		___m_ThisState_2 = value;
	}
};


// UnityEngine.Experimental.TerrainAPI.TerrainUtility_TerrainMap_TileCoord
struct  TileCoord_t51EDF1EA1A3A7F9C1D85C186E7A7954535C225BA 
{
public:
	// System.Int32 UnityEngine.Experimental.TerrainAPI.TerrainUtility_TerrainMap_TileCoord::tileX
	int32_t ___tileX_0;
	// System.Int32 UnityEngine.Experimental.TerrainAPI.TerrainUtility_TerrainMap_TileCoord::tileZ
	int32_t ___tileZ_1;

public:
	inline static int32_t get_offset_of_tileX_0() { return static_cast<int32_t>(offsetof(TileCoord_t51EDF1EA1A3A7F9C1D85C186E7A7954535C225BA, ___tileX_0)); }
	inline int32_t get_tileX_0() const { return ___tileX_0; }
	inline int32_t* get_address_of_tileX_0() { return &___tileX_0; }
	inline void set_tileX_0(int32_t value)
	{
		___tileX_0 = value;
	}

	inline static int32_t get_offset_of_tileZ_1() { return static_cast<int32_t>(offsetof(TileCoord_t51EDF1EA1A3A7F9C1D85C186E7A7954535C225BA, ___tileZ_1)); }
	inline int32_t get_tileZ_1() const { return ___tileZ_1; }
	inline int32_t* get_address_of_tileZ_1() { return &___tileZ_1; }
	inline void set_tileZ_1(int32_t value)
	{
		___tileZ_1 = value;
	}
};


// UnityEngine.Experimental.XR.FrameReceivedEventArgs
struct  FrameReceivedEventArgs_t4637B6D2FC28197602B18C1815C4A778645479DD 
{
public:
	// UnityEngine.Experimental.XR.XRCameraSubsystem UnityEngine.Experimental.XR.FrameReceivedEventArgs::m_CameraSubsystem
	XRCameraSubsystem_t9271DB5D8FEDD3431246FCB6D9257A940246E701 * ___m_CameraSubsystem_0;

public:
	inline static int32_t get_offset_of_m_CameraSubsystem_0() { return static_cast<int32_t>(offsetof(FrameReceivedEventArgs_t4637B6D2FC28197602B18C1815C4A778645479DD, ___m_CameraSubsystem_0)); }
	inline XRCameraSubsystem_t9271DB5D8FEDD3431246FCB6D9257A940246E701 * get_m_CameraSubsystem_0() const { return ___m_CameraSubsystem_0; }
	inline XRCameraSubsystem_t9271DB5D8FEDD3431246FCB6D9257A940246E701 ** get_address_of_m_CameraSubsystem_0() { return &___m_CameraSubsystem_0; }
	inline void set_m_CameraSubsystem_0(XRCameraSubsystem_t9271DB5D8FEDD3431246FCB6D9257A940246E701 * value)
	{
		___m_CameraSubsystem_0 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___m_CameraSubsystem_0), (void*)value);
	}
};

// Native definition for P/Invoke marshalling of UnityEngine.Experimental.XR.FrameReceivedEventArgs
struct FrameReceivedEventArgs_t4637B6D2FC28197602B18C1815C4A778645479DD_marshaled_pinvoke
{
	XRCameraSubsystem_t9271DB5D8FEDD3431246FCB6D9257A940246E701 * ___m_CameraSubsystem_0;
};
// Native definition for COM marshalling of UnityEngine.Experimental.XR.FrameReceivedEventArgs
struct FrameReceivedEventArgs_t4637B6D2FC28197602B18C1815C4A778645479DD_marshaled_com
{
	XRCameraSubsystem_t9271DB5D8FEDD3431246FCB6D9257A940246E701 * ___m_CameraSubsystem_0;
};

// UnityEngine.Experimental.XR.PointCloudUpdatedEventArgs
struct  PointCloudUpdatedEventArgs_tE7E1E32A6042806B927B110250C0D4FE755C6B07 
{
public:
	// UnityEngine.Experimental.XR.XRDepthSubsystem UnityEngine.Experimental.XR.PointCloudUpdatedEventArgs::m_DepthSubsystem
	XRDepthSubsystem_t1F42ECBC6085EFA6909640A9521920C55444D089 * ___m_DepthSubsystem_0;

public:
	inline static int32_t get_offset_of_m_DepthSubsystem_0() { return static_cast<int32_t>(offsetof(PointCloudUpdatedEventArgs_tE7E1E32A6042806B927B110250C0D4FE755C6B07, ___m_DepthSubsystem_0)); }
	inline XRDepthSubsystem_t1F42ECBC6085EFA6909640A9521920C55444D089 * get_m_DepthSubsystem_0() const { return ___m_DepthSubsystem_0; }
	inline XRDepthSubsystem_t1F42ECBC6085EFA6909640A9521920C55444D089 ** get_address_of_m_DepthSubsystem_0() { return &___m_DepthSubsystem_0; }
	inline void set_m_DepthSubsystem_0(XRDepthSubsystem_t1F42ECBC6085EFA6909640A9521920C55444D089 * value)
	{
		___m_DepthSubsystem_0 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___m_DepthSubsystem_0), (void*)value);
	}
};

// Native definition for P/Invoke marshalling of UnityEngine.Experimental.XR.PointCloudUpdatedEventArgs
struct PointCloudUpdatedEventArgs_tE7E1E32A6042806B927B110250C0D4FE755C6B07_marshaled_pinvoke
{
	XRDepthSubsystem_t1F42ECBC6085EFA6909640A9521920C55444D089 * ___m_DepthSubsystem_0;
};
// Native definition for COM marshalling of UnityEngine.Experimental.XR.PointCloudUpdatedEventArgs
struct PointCloudUpdatedEventArgs_tE7E1E32A6042806B927B110250C0D4FE755C6B07_marshaled_com
{
	XRDepthSubsystem_t1F42ECBC6085EFA6909640A9521920C55444D089 * ___m_DepthSubsystem_0;
};

// UnityEngine.Experimental.XR.TrackableId
struct  TrackableId_tA539F57E82A04D410FE11E10ACC830CF7CD71F7B 
{
public:
	// System.UInt64 UnityEngine.Experimental.XR.TrackableId::m_SubId1
	uint64_t ___m_SubId1_1;
	// System.UInt64 UnityEngine.Experimental.XR.TrackableId::m_SubId2
	uint64_t ___m_SubId2_2;

public:
	inline static int32_t get_offset_of_m_SubId1_1() { return static_cast<int32_t>(offsetof(TrackableId_tA539F57E82A04D410FE11E10ACC830CF7CD71F7B, ___m_SubId1_1)); }
	inline uint64_t get_m_SubId1_1() const { return ___m_SubId1_1; }
	inline uint64_t* get_address_of_m_SubId1_1() { return &___m_SubId1_1; }
	inline void set_m_SubId1_1(uint64_t value)
	{
		___m_SubId1_1 = value;
	}

	inline static int32_t get_offset_of_m_SubId2_2() { return static_cast<int32_t>(offsetof(TrackableId_tA539F57E82A04D410FE11E10ACC830CF7CD71F7B, ___m_SubId2_2)); }
	inline uint64_t get_m_SubId2_2() const { return ___m_SubId2_2; }
	inline uint64_t* get_address_of_m_SubId2_2() { return &___m_SubId2_2; }
	inline void set_m_SubId2_2(uint64_t value)
	{
		___m_SubId2_2 = value;
	}
};

struct TrackableId_tA539F57E82A04D410FE11E10ACC830CF7CD71F7B_StaticFields
{
public:
	// UnityEngine.Experimental.XR.TrackableId UnityEngine.Experimental.XR.TrackableId::s_InvalidId
	TrackableId_tA539F57E82A04D410FE11E10ACC830CF7CD71F7B  ___s_InvalidId_0;

public:
	inline static int32_t get_offset_of_s_InvalidId_0() { return static_cast<int32_t>(offsetof(TrackableId_tA539F57E82A04D410FE11E10ACC830CF7CD71F7B_StaticFields, ___s_InvalidId_0)); }
	inline TrackableId_tA539F57E82A04D410FE11E10ACC830CF7CD71F7B  get_s_InvalidId_0() const { return ___s_InvalidId_0; }
	inline TrackableId_tA539F57E82A04D410FE11E10ACC830CF7CD71F7B * get_address_of_s_InvalidId_0() { return &___s_InvalidId_0; }
	inline void set_s_InvalidId_0(TrackableId_tA539F57E82A04D410FE11E10ACC830CF7CD71F7B  value)
	{
		___s_InvalidId_0 = value;
	}
};


// UnityEngine.Matrix4x4
struct  Matrix4x4_t6BF60F70C9169DF14C9D2577672A44224B236ECA 
{
public:
	// System.Single UnityEngine.Matrix4x4::m00
	float ___m00_0;
	// System.Single UnityEngine.Matrix4x4::m10
	float ___m10_1;
	// System.Single UnityEngine.Matrix4x4::m20
	float ___m20_2;
	// System.Single UnityEngine.Matrix4x4::m30
	float ___m30_3;
	// System.Single UnityEngine.Matrix4x4::m01
	float ___m01_4;
	// System.Single UnityEngine.Matrix4x4::m11
	float ___m11_5;
	// System.Single UnityEngine.Matrix4x4::m21
	float ___m21_6;
	// System.Single UnityEngine.Matrix4x4::m31
	float ___m31_7;
	// System.Single UnityEngine.Matrix4x4::m02
	float ___m02_8;
	// System.Single UnityEngine.Matrix4x4::m12
	float ___m12_9;
	// System.Single UnityEngine.Matrix4x4::m22
	float ___m22_10;
	// System.Single UnityEngine.Matrix4x4::m32
	float ___m32_11;
	// System.Single UnityEngine.Matrix4x4::m03
	float ___m03_12;
	// System.Single UnityEngine.Matrix4x4::m13
	float ___m13_13;
	// System.Single UnityEngine.Matrix4x4::m23
	float ___m23_14;
	// System.Single UnityEngine.Matrix4x4::m33
	float ___m33_15;

public:
	inline static int32_t get_offset_of_m00_0() { return static_cast<int32_t>(offsetof(Matrix4x4_t6BF60F70C9169DF14C9D2577672A44224B236ECA, ___m00_0)); }
	inline float get_m00_0() const { return ___m00_0; }
	inline float* get_address_of_m00_0() { return &___m00_0; }
	inline void set_m00_0(float value)
	{
		___m00_0 = value;
	}

	inline static int32_t get_offset_of_m10_1() { return static_cast<int32_t>(offsetof(Matrix4x4_t6BF60F70C9169DF14C9D2577672A44224B236ECA, ___m10_1)); }
	inline float get_m10_1() const { return ___m10_1; }
	inline float* get_address_of_m10_1() { return &___m10_1; }
	inline void set_m10_1(float value)
	{
		___m10_1 = value;
	}

	inline static int32_t get_offset_of_m20_2() { return static_cast<int32_t>(offsetof(Matrix4x4_t6BF60F70C9169DF14C9D2577672A44224B236ECA, ___m20_2)); }
	inline float get_m20_2() const { return ___m20_2; }
	inline float* get_address_of_m20_2() { return &___m20_2; }
	inline void set_m20_2(float value)
	{
		___m20_2 = value;
	}

	inline static int32_t get_offset_of_m30_3() { return static_cast<int32_t>(offsetof(Matrix4x4_t6BF60F70C9169DF14C9D2577672A44224B236ECA, ___m30_3)); }
	inline float get_m30_3() const { return ___m30_3; }
	inline float* get_address_of_m30_3() { return &___m30_3; }
	inline void set_m30_3(float value)
	{
		___m30_3 = value;
	}

	inline static int32_t get_offset_of_m01_4() { return static_cast<int32_t>(offsetof(Matrix4x4_t6BF60F70C9169DF14C9D2577672A44224B236ECA, ___m01_4)); }
	inline float get_m01_4() const { return ___m01_4; }
	inline float* get_address_of_m01_4() { return &___m01_4; }
	inline void set_m01_4(float value)
	{
		___m01_4 = value;
	}

	inline static int32_t get_offset_of_m11_5() { return static_cast<int32_t>(offsetof(Matrix4x4_t6BF60F70C9169DF14C9D2577672A44224B236ECA, ___m11_5)); }
	inline float get_m11_5() const { return ___m11_5; }
	inline float* get_address_of_m11_5() { return &___m11_5; }
	inline void set_m11_5(float value)
	{
		___m11_5 = value;
	}

	inline static int32_t get_offset_of_m21_6() { return static_cast<int32_t>(offsetof(Matrix4x4_t6BF60F70C9169DF14C9D2577672A44224B236ECA, ___m21_6)); }
	inline float get_m21_6() const { return ___m21_6; }
	inline float* get_address_of_m21_6() { return &___m21_6; }
	inline void set_m21_6(float value)
	{
		___m21_6 = value;
	}

	inline static int32_t get_offset_of_m31_7() { return static_cast<int32_t>(offsetof(Matrix4x4_t6BF60F70C9169DF14C9D2577672A44224B236ECA, ___m31_7)); }
	inline float get_m31_7() const { return ___m31_7; }
	inline float* get_address_of_m31_7() { return &___m31_7; }
	inline void set_m31_7(float value)
	{
		___m31_7 = value;
	}

	inline static int32_t get_offset_of_m02_8() { return static_cast<int32_t>(offsetof(Matrix4x4_t6BF60F70C9169DF14C9D2577672A44224B236ECA, ___m02_8)); }
	inline float get_m02_8() const { return ___m02_8; }
	inline float* get_address_of_m02_8() { return &___m02_8; }
	inline void set_m02_8(float value)
	{
		___m02_8 = value;
	}

	inline static int32_t get_offset_of_m12_9() { return static_cast<int32_t>(offsetof(Matrix4x4_t6BF60F70C9169DF14C9D2577672A44224B236ECA, ___m12_9)); }
	inline float get_m12_9() const { return ___m12_9; }
	inline float* get_address_of_m12_9() { return &___m12_9; }
	inline void set_m12_9(float value)
	{
		___m12_9 = value;
	}

	inline static int32_t get_offset_of_m22_10() { return static_cast<int32_t>(offsetof(Matrix4x4_t6BF60F70C9169DF14C9D2577672A44224B236ECA, ___m22_10)); }
	inline float get_m22_10() const { return ___m22_10; }
	inline float* get_address_of_m22_10() { return &___m22_10; }
	inline void set_m22_10(float value)
	{
		___m22_10 = value;
	}

	inline static int32_t get_offset_of_m32_11() { return static_cast<int32_t>(offsetof(Matrix4x4_t6BF60F70C9169DF14C9D2577672A44224B236ECA, ___m32_11)); }
	inline float get_m32_11() const { return ___m32_11; }
	inline float* get_address_of_m32_11() { return &___m32_11; }
	inline void set_m32_11(float value)
	{
		___m32_11 = value;
	}

	inline static int32_t get_offset_of_m03_12() { return static_cast<int32_t>(offsetof(Matrix4x4_t6BF60F70C9169DF14C9D2577672A44224B236ECA, ___m03_12)); }
	inline float get_m03_12() const { return ___m03_12; }
	inline float* get_address_of_m03_12() { return &___m03_12; }
	inline void set_m03_12(float value)
	{
		___m03_12 = value;
	}

	inline static int32_t get_offset_of_m13_13() { return static_cast<int32_t>(offsetof(Matrix4x4_t6BF60F70C9169DF14C9D2577672A44224B236ECA, ___m13_13)); }
	inline float get_m13_13() const { return ___m13_13; }
	inline float* get_address_of_m13_13() { return &___m13_13; }
	inline void set_m13_13(float value)
	{
		___m13_13 = value;
	}

	inline static int32_t get_offset_of_m23_14() { return static_cast<int32_t>(offsetof(Matrix4x4_t6BF60F70C9169DF14C9D2577672A44224B236ECA, ___m23_14)); }
	inline float get_m23_14() const { return ___m23_14; }
	inline float* get_address_of_m23_14() { return &___m23_14; }
	inline void set_m23_14(float value)
	{
		___m23_14 = value;
	}

	inline static int32_t get_offset_of_m33_15() { return static_cast<int32_t>(offsetof(Matrix4x4_t6BF60F70C9169DF14C9D2577672A44224B236ECA, ___m33_15)); }
	inline float get_m33_15() const { return ___m33_15; }
	inline float* get_address_of_m33_15() { return &___m33_15; }
	inline void set_m33_15(float value)
	{
		___m33_15 = value;
	}
};

struct Matrix4x4_t6BF60F70C9169DF14C9D2577672A44224B236ECA_StaticFields
{
public:
	// UnityEngine.Matrix4x4 UnityEngine.Matrix4x4::zeroMatrix
	Matrix4x4_t6BF60F70C9169DF14C9D2577672A44224B236ECA  ___zeroMatrix_16;
	// UnityEngine.Matrix4x4 UnityEngine.Matrix4x4::identityMatrix
	Matrix4x4_t6BF60F70C9169DF14C9D2577672A44224B236ECA  ___identityMatrix_17;

public:
	inline static int32_t get_offset_of_zeroMatrix_16() { return static_cast<int32_t>(offsetof(Matrix4x4_t6BF60F70C9169DF14C9D2577672A44224B236ECA_StaticFields, ___zeroMatrix_16)); }
	inline Matrix4x4_t6BF60F70C9169DF14C9D2577672A44224B236ECA  get_zeroMatrix_16() const { return ___zeroMatrix_16; }
	inline Matrix4x4_t6BF60F70C9169DF14C9D2577672A44224B236ECA * get_address_of_zeroMatrix_16() { return &___zeroMatrix_16; }
	inline void set_zeroMatrix_16(Matrix4x4_t6BF60F70C9169DF14C9D2577672A44224B236ECA  value)
	{
		___zeroMatrix_16 = value;
	}

	inline static int32_t get_offset_of_identityMatrix_17() { return static_cast<int32_t>(offsetof(Matrix4x4_t6BF60F70C9169DF14C9D2577672A44224B236ECA_StaticFields, ___identityMatrix_17)); }
	inline Matrix4x4_t6BF60F70C9169DF14C9D2577672A44224B236ECA  get_identityMatrix_17() const { return ___identityMatrix_17; }
	inline Matrix4x4_t6BF60F70C9169DF14C9D2577672A44224B236ECA * get_address_of_identityMatrix_17() { return &___identityMatrix_17; }
	inline void set_identityMatrix_17(Matrix4x4_t6BF60F70C9169DF14C9D2577672A44224B236ECA  value)
	{
		___identityMatrix_17 = value;
	}
};


// UnityEngine.Quaternion
struct  Quaternion_t319F3319A7D43FFA5D819AD6C0A98851F0095357 
{
public:
	// System.Single UnityEngine.Quaternion::x
	float ___x_0;
	// System.Single UnityEngine.Quaternion::y
	float ___y_1;
	// System.Single UnityEngine.Quaternion::z
	float ___z_2;
	// System.Single UnityEngine.Quaternion::w
	float ___w_3;

public:
	inline static int32_t get_offset_of_x_0() { return static_cast<int32_t>(offsetof(Quaternion_t319F3319A7D43FFA5D819AD6C0A98851F0095357, ___x_0)); }
	inline float get_x_0() const { return ___x_0; }
	inline float* get_address_of_x_0() { return &___x_0; }
	inline void set_x_0(float value)
	{
		___x_0 = value;
	}

	inline static int32_t get_offset_of_y_1() { return static_cast<int32_t>(offsetof(Quaternion_t319F3319A7D43FFA5D819AD6C0A98851F0095357, ___y_1)); }
	inline float get_y_1() const { return ___y_1; }
	inline float* get_address_of_y_1() { return &___y_1; }
	inline void set_y_1(float value)
	{
		___y_1 = value;
	}

	inline static int32_t get_offset_of_z_2() { return static_cast<int32_t>(offsetof(Quaternion_t319F3319A7D43FFA5D819AD6C0A98851F0095357, ___z_2)); }
	inline float get_z_2() const { return ___z_2; }
	inline float* get_address_of_z_2() { return &___z_2; }
	inline void set_z_2(float value)
	{
		___z_2 = value;
	}

	inline static int32_t get_offset_of_w_3() { return static_cast<int32_t>(offsetof(Quaternion_t319F3319A7D43FFA5D819AD6C0A98851F0095357, ___w_3)); }
	inline float get_w_3() const { return ___w_3; }
	inline float* get_address_of_w_3() { return &___w_3; }
	inline void set_w_3(float value)
	{
		___w_3 = value;
	}
};

struct Quaternion_t319F3319A7D43FFA5D819AD6C0A98851F0095357_StaticFields
{
public:
	// UnityEngine.Quaternion UnityEngine.Quaternion::identityQuaternion
	Quaternion_t319F3319A7D43FFA5D819AD6C0A98851F0095357  ___identityQuaternion_4;

public:
	inline static int32_t get_offset_of_identityQuaternion_4() { return static_cast<int32_t>(offsetof(Quaternion_t319F3319A7D43FFA5D819AD6C0A98851F0095357_StaticFields, ___identityQuaternion_4)); }
	inline Quaternion_t319F3319A7D43FFA5D819AD6C0A98851F0095357  get_identityQuaternion_4() const { return ___identityQuaternion_4; }
	inline Quaternion_t319F3319A7D43FFA5D819AD6C0A98851F0095357 * get_address_of_identityQuaternion_4() { return &___identityQuaternion_4; }
	inline void set_identityQuaternion_4(Quaternion_t319F3319A7D43FFA5D819AD6C0A98851F0095357  value)
	{
		___identityQuaternion_4 = value;
	}
};


// UnityEngine.Rect
struct  Rect_t35B976DE901B5423C11705E156938EA27AB402CE 
{
public:
	// System.Single UnityEngine.Rect::m_XMin
	float ___m_XMin_0;
	// System.Single UnityEngine.Rect::m_YMin
	float ___m_YMin_1;
	// System.Single UnityEngine.Rect::m_Width
	float ___m_Width_2;
	// System.Single UnityEngine.Rect::m_Height
	float ___m_Height_3;

public:
	inline static int32_t get_offset_of_m_XMin_0() { return static_cast<int32_t>(offsetof(Rect_t35B976DE901B5423C11705E156938EA27AB402CE, ___m_XMin_0)); }
	inline float get_m_XMin_0() const { return ___m_XMin_0; }
	inline float* get_address_of_m_XMin_0() { return &___m_XMin_0; }
	inline void set_m_XMin_0(float value)
	{
		___m_XMin_0 = value;
	}

	inline static int32_t get_offset_of_m_YMin_1() { return static_cast<int32_t>(offsetof(Rect_t35B976DE901B5423C11705E156938EA27AB402CE, ___m_YMin_1)); }
	inline float get_m_YMin_1() const { return ___m_YMin_1; }
	inline float* get_address_of_m_YMin_1() { return &___m_YMin_1; }
	inline void set_m_YMin_1(float value)
	{
		___m_YMin_1 = value;
	}

	inline static int32_t get_offset_of_m_Width_2() { return static_cast<int32_t>(offsetof(Rect_t35B976DE901B5423C11705E156938EA27AB402CE, ___m_Width_2)); }
	inline float get_m_Width_2() const { return ___m_Width_2; }
	inline float* get_address_of_m_Width_2() { return &___m_Width_2; }
	inline void set_m_Width_2(float value)
	{
		___m_Width_2 = value;
	}

	inline static int32_t get_offset_of_m_Height_3() { return static_cast<int32_t>(offsetof(Rect_t35B976DE901B5423C11705E156938EA27AB402CE, ___m_Height_3)); }
	inline float get_m_Height_3() const { return ___m_Height_3; }
	inline float* get_address_of_m_Height_3() { return &___m_Height_3; }
	inline void set_m_Height_3(float value)
	{
		___m_Height_3 = value;
	}
};


// UnityEngine.RectInt
struct  RectInt_t595A63F7EE2BC91A4D2DE5403C5FE94D3C3A6F7A 
{
public:
	// System.Int32 UnityEngine.RectInt::m_XMin
	int32_t ___m_XMin_0;
	// System.Int32 UnityEngine.RectInt::m_YMin
	int32_t ___m_YMin_1;
	// System.Int32 UnityEngine.RectInt::m_Width
	int32_t ___m_Width_2;
	// System.Int32 UnityEngine.RectInt::m_Height
	int32_t ___m_Height_3;

public:
	inline static int32_t get_offset_of_m_XMin_0() { return static_cast<int32_t>(offsetof(RectInt_t595A63F7EE2BC91A4D2DE5403C5FE94D3C3A6F7A, ___m_XMin_0)); }
	inline int32_t get_m_XMin_0() const { return ___m_XMin_0; }
	inline int32_t* get_address_of_m_XMin_0() { return &___m_XMin_0; }
	inline void set_m_XMin_0(int32_t value)
	{
		___m_XMin_0 = value;
	}

	inline static int32_t get_offset_of_m_YMin_1() { return static_cast<int32_t>(offsetof(RectInt_t595A63F7EE2BC91A4D2DE5403C5FE94D3C3A6F7A, ___m_YMin_1)); }
	inline int32_t get_m_YMin_1() const { return ___m_YMin_1; }
	inline int32_t* get_address_of_m_YMin_1() { return &___m_YMin_1; }
	inline void set_m_YMin_1(int32_t value)
	{
		___m_YMin_1 = value;
	}

	inline static int32_t get_offset_of_m_Width_2() { return static_cast<int32_t>(offsetof(RectInt_t595A63F7EE2BC91A4D2DE5403C5FE94D3C3A6F7A, ___m_Width_2)); }
	inline int32_t get_m_Width_2() const { return ___m_Width_2; }
	inline int32_t* get_address_of_m_Width_2() { return &___m_Width_2; }
	inline void set_m_Width_2(int32_t value)
	{
		___m_Width_2 = value;
	}

	inline static int32_t get_offset_of_m_Height_3() { return static_cast<int32_t>(offsetof(RectInt_t595A63F7EE2BC91A4D2DE5403C5FE94D3C3A6F7A, ___m_Height_3)); }
	inline int32_t get_m_Height_3() const { return ___m_Height_3; }
	inline int32_t* get_address_of_m_Height_3() { return &___m_Height_3; }
	inline void set_m_Height_3(int32_t value)
	{
		___m_Height_3 = value;
	}
};


// UnityEngine.SceneManagement.Scene
struct  Scene_t942E023788C2BC9FBB7EC8356B4FB0088B2CFED2 
{
public:
	// System.Int32 UnityEngine.SceneManagement.Scene::m_Handle
	int32_t ___m_Handle_0;

public:
	inline static int32_t get_offset_of_m_Handle_0() { return static_cast<int32_t>(offsetof(Scene_t942E023788C2BC9FBB7EC8356B4FB0088B2CFED2, ___m_Handle_0)); }
	inline int32_t get_m_Handle_0() const { return ___m_Handle_0; }
	inline int32_t* get_address_of_m_Handle_0() { return &___m_Handle_0; }
	inline void set_m_Handle_0(int32_t value)
	{
		___m_Handle_0 = value;
	}
};


// UnityEngine.UI.SpriteState
struct  SpriteState_t58B9DD66A79CD69AB4CFC3AD0C41E45DC2192C0A 
{
public:
	// UnityEngine.Sprite UnityEngine.UI.SpriteState::m_HighlightedSprite
	Sprite_tCA09498D612D08DE668653AF1E9C12BF53434198 * ___m_HighlightedSprite_0;
	// UnityEngine.Sprite UnityEngine.UI.SpriteState::m_PressedSprite
	Sprite_tCA09498D612D08DE668653AF1E9C12BF53434198 * ___m_PressedSprite_1;
	// UnityEngine.Sprite UnityEngine.UI.SpriteState::m_SelectedSprite
	Sprite_tCA09498D612D08DE668653AF1E9C12BF53434198 * ___m_SelectedSprite_2;
	// UnityEngine.Sprite UnityEngine.UI.SpriteState::m_DisabledSprite
	Sprite_tCA09498D612D08DE668653AF1E9C12BF53434198 * ___m_DisabledSprite_3;

public:
	inline static int32_t get_offset_of_m_HighlightedSprite_0() { return static_cast<int32_t>(offsetof(SpriteState_t58B9DD66A79CD69AB4CFC3AD0C41E45DC2192C0A, ___m_HighlightedSprite_0)); }
	inline Sprite_tCA09498D612D08DE668653AF1E9C12BF53434198 * get_m_HighlightedSprite_0() const { return ___m_HighlightedSprite_0; }
	inline Sprite_tCA09498D612D08DE668653AF1E9C12BF53434198 ** get_address_of_m_HighlightedSprite_0() { return &___m_HighlightedSprite_0; }
	inline void set_m_HighlightedSprite_0(Sprite_tCA09498D612D08DE668653AF1E9C12BF53434198 * value)
	{
		___m_HighlightedSprite_0 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___m_HighlightedSprite_0), (void*)value);
	}

	inline static int32_t get_offset_of_m_PressedSprite_1() { return static_cast<int32_t>(offsetof(SpriteState_t58B9DD66A79CD69AB4CFC3AD0C41E45DC2192C0A, ___m_PressedSprite_1)); }
	inline Sprite_tCA09498D612D08DE668653AF1E9C12BF53434198 * get_m_PressedSprite_1() const { return ___m_PressedSprite_1; }
	inline Sprite_tCA09498D612D08DE668653AF1E9C12BF53434198 ** get_address_of_m_PressedSprite_1() { return &___m_PressedSprite_1; }
	inline void set_m_PressedSprite_1(Sprite_tCA09498D612D08DE668653AF1E9C12BF53434198 * value)
	{
		___m_PressedSprite_1 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___m_PressedSprite_1), (void*)value);
	}

	inline static int32_t get_offset_of_m_SelectedSprite_2() { return static_cast<int32_t>(offsetof(SpriteState_t58B9DD66A79CD69AB4CFC3AD0C41E45DC2192C0A, ___m_SelectedSprite_2)); }
	inline Sprite_tCA09498D612D08DE668653AF1E9C12BF53434198 * get_m_SelectedSprite_2() const { return ___m_SelectedSprite_2; }
	inline Sprite_tCA09498D612D08DE668653AF1E9C12BF53434198 ** get_address_of_m_SelectedSprite_2() { return &___m_SelectedSprite_2; }
	inline void set_m_SelectedSprite_2(Sprite_tCA09498D612D08DE668653AF1E9C12BF53434198 * value)
	{
		___m_SelectedSprite_2 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___m_SelectedSprite_2), (void*)value);
	}

	inline static int32_t get_offset_of_m_DisabledSprite_3() { return static_cast<int32_t>(offsetof(SpriteState_t58B9DD66A79CD69AB4CFC3AD0C41E45DC2192C0A, ___m_DisabledSprite_3)); }
	inline Sprite_tCA09498D612D08DE668653AF1E9C12BF53434198 * get_m_DisabledSprite_3() const { return ___m_DisabledSprite_3; }
	inline Sprite_tCA09498D612D08DE668653AF1E9C12BF53434198 ** get_address_of_m_DisabledSprite_3() { return &___m_DisabledSprite_3; }
	inline void set_m_DisabledSprite_3(Sprite_tCA09498D612D08DE668653AF1E9C12BF53434198 * value)
	{
		___m_DisabledSprite_3 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___m_DisabledSprite_3), (void*)value);
	}
};

// Native definition for P/Invoke marshalling of UnityEngine.UI.SpriteState
struct SpriteState_t58B9DD66A79CD69AB4CFC3AD0C41E45DC2192C0A_marshaled_pinvoke
{
	Sprite_tCA09498D612D08DE668653AF1E9C12BF53434198 * ___m_HighlightedSprite_0;
	Sprite_tCA09498D612D08DE668653AF1E9C12BF53434198 * ___m_PressedSprite_1;
	Sprite_tCA09498D612D08DE668653AF1E9C12BF53434198 * ___m_SelectedSprite_2;
	Sprite_tCA09498D612D08DE668653AF1E9C12BF53434198 * ___m_DisabledSprite_3;
};
// Native definition for COM marshalling of UnityEngine.UI.SpriteState
struct SpriteState_t58B9DD66A79CD69AB4CFC3AD0C41E45DC2192C0A_marshaled_com
{
	Sprite_tCA09498D612D08DE668653AF1E9C12BF53434198 * ___m_HighlightedSprite_0;
	Sprite_tCA09498D612D08DE668653AF1E9C12BF53434198 * ___m_PressedSprite_1;
	Sprite_tCA09498D612D08DE668653AF1E9C12BF53434198 * ___m_SelectedSprite_2;
	Sprite_tCA09498D612D08DE668653AF1E9C12BF53434198 * ___m_DisabledSprite_3;
};

// UnityEngine.UILineInfo
struct  UILineInfo_t0AF27251CA07CEE2BC0C1FEF752245596B8033E6 
{
public:
	// System.Int32 UnityEngine.UILineInfo::startCharIdx
	int32_t ___startCharIdx_0;
	// System.Int32 UnityEngine.UILineInfo::height
	int32_t ___height_1;
	// System.Single UnityEngine.UILineInfo::topY
	float ___topY_2;
	// System.Single UnityEngine.UILineInfo::leading
	float ___leading_3;

public:
	inline static int32_t get_offset_of_startCharIdx_0() { return static_cast<int32_t>(offsetof(UILineInfo_t0AF27251CA07CEE2BC0C1FEF752245596B8033E6, ___startCharIdx_0)); }
	inline int32_t get_startCharIdx_0() const { return ___startCharIdx_0; }
	inline int32_t* get_address_of_startCharIdx_0() { return &___startCharIdx_0; }
	inline void set_startCharIdx_0(int32_t value)
	{
		___startCharIdx_0 = value;
	}

	inline static int32_t get_offset_of_height_1() { return static_cast<int32_t>(offsetof(UILineInfo_t0AF27251CA07CEE2BC0C1FEF752245596B8033E6, ___height_1)); }
	inline int32_t get_height_1() const { return ___height_1; }
	inline int32_t* get_address_of_height_1() { return &___height_1; }
	inline void set_height_1(int32_t value)
	{
		___height_1 = value;
	}

	inline static int32_t get_offset_of_topY_2() { return static_cast<int32_t>(offsetof(UILineInfo_t0AF27251CA07CEE2BC0C1FEF752245596B8033E6, ___topY_2)); }
	inline float get_topY_2() const { return ___topY_2; }
	inline float* get_address_of_topY_2() { return &___topY_2; }
	inline void set_topY_2(float value)
	{
		___topY_2 = value;
	}

	inline static int32_t get_offset_of_leading_3() { return static_cast<int32_t>(offsetof(UILineInfo_t0AF27251CA07CEE2BC0C1FEF752245596B8033E6, ___leading_3)); }
	inline float get_leading_3() const { return ___leading_3; }
	inline float* get_address_of_leading_3() { return &___leading_3; }
	inline void set_leading_3(float value)
	{
		___leading_3 = value;
	}
};


// UnityEngine.UnitySynchronizationContext_WorkRequest
struct  WorkRequest_t0247B62D135204EAA95FC0B2EC829CB27B433F94 
{
public:
	// System.Threading.SendOrPostCallback UnityEngine.UnitySynchronizationContext_WorkRequest::m_DelagateCallback
	SendOrPostCallback_t3F9C0164860E4AA5138DF8B4488DFB0D33147F01 * ___m_DelagateCallback_0;
	// System.Object UnityEngine.UnitySynchronizationContext_WorkRequest::m_DelagateState
	RuntimeObject * ___m_DelagateState_1;
	// System.Threading.ManualResetEvent UnityEngine.UnitySynchronizationContext_WorkRequest::m_WaitHandle
	ManualResetEvent_tDFAF117B200ECA4CCF4FD09593F949A016D55408 * ___m_WaitHandle_2;

public:
	inline static int32_t get_offset_of_m_DelagateCallback_0() { return static_cast<int32_t>(offsetof(WorkRequest_t0247B62D135204EAA95FC0B2EC829CB27B433F94, ___m_DelagateCallback_0)); }
	inline SendOrPostCallback_t3F9C0164860E4AA5138DF8B4488DFB0D33147F01 * get_m_DelagateCallback_0() const { return ___m_DelagateCallback_0; }
	inline SendOrPostCallback_t3F9C0164860E4AA5138DF8B4488DFB0D33147F01 ** get_address_of_m_DelagateCallback_0() { return &___m_DelagateCallback_0; }
	inline void set_m_DelagateCallback_0(SendOrPostCallback_t3F9C0164860E4AA5138DF8B4488DFB0D33147F01 * value)
	{
		___m_DelagateCallback_0 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___m_DelagateCallback_0), (void*)value);
	}

	inline static int32_t get_offset_of_m_DelagateState_1() { return static_cast<int32_t>(offsetof(WorkRequest_t0247B62D135204EAA95FC0B2EC829CB27B433F94, ___m_DelagateState_1)); }
	inline RuntimeObject * get_m_DelagateState_1() const { return ___m_DelagateState_1; }
	inline RuntimeObject ** get_address_of_m_DelagateState_1() { return &___m_DelagateState_1; }
	inline void set_m_DelagateState_1(RuntimeObject * value)
	{
		___m_DelagateState_1 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___m_DelagateState_1), (void*)value);
	}

	inline static int32_t get_offset_of_m_WaitHandle_2() { return static_cast<int32_t>(offsetof(WorkRequest_t0247B62D135204EAA95FC0B2EC829CB27B433F94, ___m_WaitHandle_2)); }
	inline ManualResetEvent_tDFAF117B200ECA4CCF4FD09593F949A016D55408 * get_m_WaitHandle_2() const { return ___m_WaitHandle_2; }
	inline ManualResetEvent_tDFAF117B200ECA4CCF4FD09593F949A016D55408 ** get_address_of_m_WaitHandle_2() { return &___m_WaitHandle_2; }
	inline void set_m_WaitHandle_2(ManualResetEvent_tDFAF117B200ECA4CCF4FD09593F949A016D55408 * value)
	{
		___m_WaitHandle_2 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___m_WaitHandle_2), (void*)value);
	}
};

// Native definition for P/Invoke marshalling of UnityEngine.UnitySynchronizationContext/WorkRequest
struct WorkRequest_t0247B62D135204EAA95FC0B2EC829CB27B433F94_marshaled_pinvoke
{
	Il2CppMethodPointer ___m_DelagateCallback_0;
	Il2CppIUnknown* ___m_DelagateState_1;
	ManualResetEvent_tDFAF117B200ECA4CCF4FD09593F949A016D55408 * ___m_WaitHandle_2;
};
// Native definition for COM marshalling of UnityEngine.UnitySynchronizationContext/WorkRequest
struct WorkRequest_t0247B62D135204EAA95FC0B2EC829CB27B433F94_marshaled_com
{
	Il2CppMethodPointer ___m_DelagateCallback_0;
	Il2CppIUnknown* ___m_DelagateState_1;
	ManualResetEvent_tDFAF117B200ECA4CCF4FD09593F949A016D55408 * ___m_WaitHandle_2;
};

// UnityEngine.Vector2
struct  Vector2_tA85D2DD88578276CA8A8796756458277E72D073D 
{
public:
	// System.Single UnityEngine.Vector2::x
	float ___x_0;
	// System.Single UnityEngine.Vector2::y
	float ___y_1;

public:
	inline static int32_t get_offset_of_x_0() { return static_cast<int32_t>(offsetof(Vector2_tA85D2DD88578276CA8A8796756458277E72D073D, ___x_0)); }
	inline float get_x_0() const { return ___x_0; }
	inline float* get_address_of_x_0() { return &___x_0; }
	inline void set_x_0(float value)
	{
		___x_0 = value;
	}

	inline static int32_t get_offset_of_y_1() { return static_cast<int32_t>(offsetof(Vector2_tA85D2DD88578276CA8A8796756458277E72D073D, ___y_1)); }
	inline float get_y_1() const { return ___y_1; }
	inline float* get_address_of_y_1() { return &___y_1; }
	inline void set_y_1(float value)
	{
		___y_1 = value;
	}
};

struct Vector2_tA85D2DD88578276CA8A8796756458277E72D073D_StaticFields
{
public:
	// UnityEngine.Vector2 UnityEngine.Vector2::zeroVector
	Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  ___zeroVector_2;
	// UnityEngine.Vector2 UnityEngine.Vector2::oneVector
	Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  ___oneVector_3;
	// UnityEngine.Vector2 UnityEngine.Vector2::upVector
	Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  ___upVector_4;
	// UnityEngine.Vector2 UnityEngine.Vector2::downVector
	Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  ___downVector_5;
	// UnityEngine.Vector2 UnityEngine.Vector2::leftVector
	Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  ___leftVector_6;
	// UnityEngine.Vector2 UnityEngine.Vector2::rightVector
	Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  ___rightVector_7;
	// UnityEngine.Vector2 UnityEngine.Vector2::positiveInfinityVector
	Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  ___positiveInfinityVector_8;
	// UnityEngine.Vector2 UnityEngine.Vector2::negativeInfinityVector
	Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  ___negativeInfinityVector_9;

public:
	inline static int32_t get_offset_of_zeroVector_2() { return static_cast<int32_t>(offsetof(Vector2_tA85D2DD88578276CA8A8796756458277E72D073D_StaticFields, ___zeroVector_2)); }
	inline Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  get_zeroVector_2() const { return ___zeroVector_2; }
	inline Vector2_tA85D2DD88578276CA8A8796756458277E72D073D * get_address_of_zeroVector_2() { return &___zeroVector_2; }
	inline void set_zeroVector_2(Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  value)
	{
		___zeroVector_2 = value;
	}

	inline static int32_t get_offset_of_oneVector_3() { return static_cast<int32_t>(offsetof(Vector2_tA85D2DD88578276CA8A8796756458277E72D073D_StaticFields, ___oneVector_3)); }
	inline Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  get_oneVector_3() const { return ___oneVector_3; }
	inline Vector2_tA85D2DD88578276CA8A8796756458277E72D073D * get_address_of_oneVector_3() { return &___oneVector_3; }
	inline void set_oneVector_3(Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  value)
	{
		___oneVector_3 = value;
	}

	inline static int32_t get_offset_of_upVector_4() { return static_cast<int32_t>(offsetof(Vector2_tA85D2DD88578276CA8A8796756458277E72D073D_StaticFields, ___upVector_4)); }
	inline Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  get_upVector_4() const { return ___upVector_4; }
	inline Vector2_tA85D2DD88578276CA8A8796756458277E72D073D * get_address_of_upVector_4() { return &___upVector_4; }
	inline void set_upVector_4(Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  value)
	{
		___upVector_4 = value;
	}

	inline static int32_t get_offset_of_downVector_5() { return static_cast<int32_t>(offsetof(Vector2_tA85D2DD88578276CA8A8796756458277E72D073D_StaticFields, ___downVector_5)); }
	inline Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  get_downVector_5() const { return ___downVector_5; }
	inline Vector2_tA85D2DD88578276CA8A8796756458277E72D073D * get_address_of_downVector_5() { return &___downVector_5; }
	inline void set_downVector_5(Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  value)
	{
		___downVector_5 = value;
	}

	inline static int32_t get_offset_of_leftVector_6() { return static_cast<int32_t>(offsetof(Vector2_tA85D2DD88578276CA8A8796756458277E72D073D_StaticFields, ___leftVector_6)); }
	inline Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  get_leftVector_6() const { return ___leftVector_6; }
	inline Vector2_tA85D2DD88578276CA8A8796756458277E72D073D * get_address_of_leftVector_6() { return &___leftVector_6; }
	inline void set_leftVector_6(Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  value)
	{
		___leftVector_6 = value;
	}

	inline static int32_t get_offset_of_rightVector_7() { return static_cast<int32_t>(offsetof(Vector2_tA85D2DD88578276CA8A8796756458277E72D073D_StaticFields, ___rightVector_7)); }
	inline Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  get_rightVector_7() const { return ___rightVector_7; }
	inline Vector2_tA85D2DD88578276CA8A8796756458277E72D073D * get_address_of_rightVector_7() { return &___rightVector_7; }
	inline void set_rightVector_7(Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  value)
	{
		___rightVector_7 = value;
	}

	inline static int32_t get_offset_of_positiveInfinityVector_8() { return static_cast<int32_t>(offsetof(Vector2_tA85D2DD88578276CA8A8796756458277E72D073D_StaticFields, ___positiveInfinityVector_8)); }
	inline Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  get_positiveInfinityVector_8() const { return ___positiveInfinityVector_8; }
	inline Vector2_tA85D2DD88578276CA8A8796756458277E72D073D * get_address_of_positiveInfinityVector_8() { return &___positiveInfinityVector_8; }
	inline void set_positiveInfinityVector_8(Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  value)
	{
		___positiveInfinityVector_8 = value;
	}

	inline static int32_t get_offset_of_negativeInfinityVector_9() { return static_cast<int32_t>(offsetof(Vector2_tA85D2DD88578276CA8A8796756458277E72D073D_StaticFields, ___negativeInfinityVector_9)); }
	inline Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  get_negativeInfinityVector_9() const { return ___negativeInfinityVector_9; }
	inline Vector2_tA85D2DD88578276CA8A8796756458277E72D073D * get_address_of_negativeInfinityVector_9() { return &___negativeInfinityVector_9; }
	inline void set_negativeInfinityVector_9(Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  value)
	{
		___negativeInfinityVector_9 = value;
	}
};


// UnityEngine.Vector3
struct  Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720 
{
public:
	// System.Single UnityEngine.Vector3::x
	float ___x_2;
	// System.Single UnityEngine.Vector3::y
	float ___y_3;
	// System.Single UnityEngine.Vector3::z
	float ___z_4;

public:
	inline static int32_t get_offset_of_x_2() { return static_cast<int32_t>(offsetof(Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720, ___x_2)); }
	inline float get_x_2() const { return ___x_2; }
	inline float* get_address_of_x_2() { return &___x_2; }
	inline void set_x_2(float value)
	{
		___x_2 = value;
	}

	inline static int32_t get_offset_of_y_3() { return static_cast<int32_t>(offsetof(Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720, ___y_3)); }
	inline float get_y_3() const { return ___y_3; }
	inline float* get_address_of_y_3() { return &___y_3; }
	inline void set_y_3(float value)
	{
		___y_3 = value;
	}

	inline static int32_t get_offset_of_z_4() { return static_cast<int32_t>(offsetof(Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720, ___z_4)); }
	inline float get_z_4() const { return ___z_4; }
	inline float* get_address_of_z_4() { return &___z_4; }
	inline void set_z_4(float value)
	{
		___z_4 = value;
	}
};

struct Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720_StaticFields
{
public:
	// UnityEngine.Vector3 UnityEngine.Vector3::zeroVector
	Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  ___zeroVector_5;
	// UnityEngine.Vector3 UnityEngine.Vector3::oneVector
	Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  ___oneVector_6;
	// UnityEngine.Vector3 UnityEngine.Vector3::upVector
	Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  ___upVector_7;
	// UnityEngine.Vector3 UnityEngine.Vector3::downVector
	Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  ___downVector_8;
	// UnityEngine.Vector3 UnityEngine.Vector3::leftVector
	Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  ___leftVector_9;
	// UnityEngine.Vector3 UnityEngine.Vector3::rightVector
	Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  ___rightVector_10;
	// UnityEngine.Vector3 UnityEngine.Vector3::forwardVector
	Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  ___forwardVector_11;
	// UnityEngine.Vector3 UnityEngine.Vector3::backVector
	Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  ___backVector_12;
	// UnityEngine.Vector3 UnityEngine.Vector3::positiveInfinityVector
	Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  ___positiveInfinityVector_13;
	// UnityEngine.Vector3 UnityEngine.Vector3::negativeInfinityVector
	Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  ___negativeInfinityVector_14;

public:
	inline static int32_t get_offset_of_zeroVector_5() { return static_cast<int32_t>(offsetof(Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720_StaticFields, ___zeroVector_5)); }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  get_zeroVector_5() const { return ___zeroVector_5; }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720 * get_address_of_zeroVector_5() { return &___zeroVector_5; }
	inline void set_zeroVector_5(Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  value)
	{
		___zeroVector_5 = value;
	}

	inline static int32_t get_offset_of_oneVector_6() { return static_cast<int32_t>(offsetof(Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720_StaticFields, ___oneVector_6)); }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  get_oneVector_6() const { return ___oneVector_6; }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720 * get_address_of_oneVector_6() { return &___oneVector_6; }
	inline void set_oneVector_6(Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  value)
	{
		___oneVector_6 = value;
	}

	inline static int32_t get_offset_of_upVector_7() { return static_cast<int32_t>(offsetof(Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720_StaticFields, ___upVector_7)); }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  get_upVector_7() const { return ___upVector_7; }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720 * get_address_of_upVector_7() { return &___upVector_7; }
	inline void set_upVector_7(Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  value)
	{
		___upVector_7 = value;
	}

	inline static int32_t get_offset_of_downVector_8() { return static_cast<int32_t>(offsetof(Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720_StaticFields, ___downVector_8)); }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  get_downVector_8() const { return ___downVector_8; }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720 * get_address_of_downVector_8() { return &___downVector_8; }
	inline void set_downVector_8(Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  value)
	{
		___downVector_8 = value;
	}

	inline static int32_t get_offset_of_leftVector_9() { return static_cast<int32_t>(offsetof(Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720_StaticFields, ___leftVector_9)); }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  get_leftVector_9() const { return ___leftVector_9; }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720 * get_address_of_leftVector_9() { return &___leftVector_9; }
	inline void set_leftVector_9(Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  value)
	{
		___leftVector_9 = value;
	}

	inline static int32_t get_offset_of_rightVector_10() { return static_cast<int32_t>(offsetof(Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720_StaticFields, ___rightVector_10)); }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  get_rightVector_10() const { return ___rightVector_10; }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720 * get_address_of_rightVector_10() { return &___rightVector_10; }
	inline void set_rightVector_10(Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  value)
	{
		___rightVector_10 = value;
	}

	inline static int32_t get_offset_of_forwardVector_11() { return static_cast<int32_t>(offsetof(Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720_StaticFields, ___forwardVector_11)); }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  get_forwardVector_11() const { return ___forwardVector_11; }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720 * get_address_of_forwardVector_11() { return &___forwardVector_11; }
	inline void set_forwardVector_11(Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  value)
	{
		___forwardVector_11 = value;
	}

	inline static int32_t get_offset_of_backVector_12() { return static_cast<int32_t>(offsetof(Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720_StaticFields, ___backVector_12)); }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  get_backVector_12() const { return ___backVector_12; }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720 * get_address_of_backVector_12() { return &___backVector_12; }
	inline void set_backVector_12(Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  value)
	{
		___backVector_12 = value;
	}

	inline static int32_t get_offset_of_positiveInfinityVector_13() { return static_cast<int32_t>(offsetof(Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720_StaticFields, ___positiveInfinityVector_13)); }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  get_positiveInfinityVector_13() const { return ___positiveInfinityVector_13; }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720 * get_address_of_positiveInfinityVector_13() { return &___positiveInfinityVector_13; }
	inline void set_positiveInfinityVector_13(Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  value)
	{
		___positiveInfinityVector_13 = value;
	}

	inline static int32_t get_offset_of_negativeInfinityVector_14() { return static_cast<int32_t>(offsetof(Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720_StaticFields, ___negativeInfinityVector_14)); }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  get_negativeInfinityVector_14() const { return ___negativeInfinityVector_14; }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720 * get_address_of_negativeInfinityVector_14() { return &___negativeInfinityVector_14; }
	inline void set_negativeInfinityVector_14(Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  value)
	{
		___negativeInfinityVector_14 = value;
	}
};


// UnityEngine.Vector3Int
struct  Vector3Int_tA843C5F8C2EB42492786C5AF82C3E1F4929942B4 
{
public:
	// System.Int32 UnityEngine.Vector3Int::m_X
	int32_t ___m_X_0;
	// System.Int32 UnityEngine.Vector3Int::m_Y
	int32_t ___m_Y_1;
	// System.Int32 UnityEngine.Vector3Int::m_Z
	int32_t ___m_Z_2;

public:
	inline static int32_t get_offset_of_m_X_0() { return static_cast<int32_t>(offsetof(Vector3Int_tA843C5F8C2EB42492786C5AF82C3E1F4929942B4, ___m_X_0)); }
	inline int32_t get_m_X_0() const { return ___m_X_0; }
	inline int32_t* get_address_of_m_X_0() { return &___m_X_0; }
	inline void set_m_X_0(int32_t value)
	{
		___m_X_0 = value;
	}

	inline static int32_t get_offset_of_m_Y_1() { return static_cast<int32_t>(offsetof(Vector3Int_tA843C5F8C2EB42492786C5AF82C3E1F4929942B4, ___m_Y_1)); }
	inline int32_t get_m_Y_1() const { return ___m_Y_1; }
	inline int32_t* get_address_of_m_Y_1() { return &___m_Y_1; }
	inline void set_m_Y_1(int32_t value)
	{
		___m_Y_1 = value;
	}

	inline static int32_t get_offset_of_m_Z_2() { return static_cast<int32_t>(offsetof(Vector3Int_tA843C5F8C2EB42492786C5AF82C3E1F4929942B4, ___m_Z_2)); }
	inline int32_t get_m_Z_2() const { return ___m_Z_2; }
	inline int32_t* get_address_of_m_Z_2() { return &___m_Z_2; }
	inline void set_m_Z_2(int32_t value)
	{
		___m_Z_2 = value;
	}
};

struct Vector3Int_tA843C5F8C2EB42492786C5AF82C3E1F4929942B4_StaticFields
{
public:
	// UnityEngine.Vector3Int UnityEngine.Vector3Int::s_Zero
	Vector3Int_tA843C5F8C2EB42492786C5AF82C3E1F4929942B4  ___s_Zero_3;
	// UnityEngine.Vector3Int UnityEngine.Vector3Int::s_One
	Vector3Int_tA843C5F8C2EB42492786C5AF82C3E1F4929942B4  ___s_One_4;
	// UnityEngine.Vector3Int UnityEngine.Vector3Int::s_Up
	Vector3Int_tA843C5F8C2EB42492786C5AF82C3E1F4929942B4  ___s_Up_5;
	// UnityEngine.Vector3Int UnityEngine.Vector3Int::s_Down
	Vector3Int_tA843C5F8C2EB42492786C5AF82C3E1F4929942B4  ___s_Down_6;
	// UnityEngine.Vector3Int UnityEngine.Vector3Int::s_Left
	Vector3Int_tA843C5F8C2EB42492786C5AF82C3E1F4929942B4  ___s_Left_7;
	// UnityEngine.Vector3Int UnityEngine.Vector3Int::s_Right
	Vector3Int_tA843C5F8C2EB42492786C5AF82C3E1F4929942B4  ___s_Right_8;

public:
	inline static int32_t get_offset_of_s_Zero_3() { return static_cast<int32_t>(offsetof(Vector3Int_tA843C5F8C2EB42492786C5AF82C3E1F4929942B4_StaticFields, ___s_Zero_3)); }
	inline Vector3Int_tA843C5F8C2EB42492786C5AF82C3E1F4929942B4  get_s_Zero_3() const { return ___s_Zero_3; }
	inline Vector3Int_tA843C5F8C2EB42492786C5AF82C3E1F4929942B4 * get_address_of_s_Zero_3() { return &___s_Zero_3; }
	inline void set_s_Zero_3(Vector3Int_tA843C5F8C2EB42492786C5AF82C3E1F4929942B4  value)
	{
		___s_Zero_3 = value;
	}

	inline static int32_t get_offset_of_s_One_4() { return static_cast<int32_t>(offsetof(Vector3Int_tA843C5F8C2EB42492786C5AF82C3E1F4929942B4_StaticFields, ___s_One_4)); }
	inline Vector3Int_tA843C5F8C2EB42492786C5AF82C3E1F4929942B4  get_s_One_4() const { return ___s_One_4; }
	inline Vector3Int_tA843C5F8C2EB42492786C5AF82C3E1F4929942B4 * get_address_of_s_One_4() { return &___s_One_4; }
	inline void set_s_One_4(Vector3Int_tA843C5F8C2EB42492786C5AF82C3E1F4929942B4  value)
	{
		___s_One_4 = value;
	}

	inline static int32_t get_offset_of_s_Up_5() { return static_cast<int32_t>(offsetof(Vector3Int_tA843C5F8C2EB42492786C5AF82C3E1F4929942B4_StaticFields, ___s_Up_5)); }
	inline Vector3Int_tA843C5F8C2EB42492786C5AF82C3E1F4929942B4  get_s_Up_5() const { return ___s_Up_5; }
	inline Vector3Int_tA843C5F8C2EB42492786C5AF82C3E1F4929942B4 * get_address_of_s_Up_5() { return &___s_Up_5; }
	inline void set_s_Up_5(Vector3Int_tA843C5F8C2EB42492786C5AF82C3E1F4929942B4  value)
	{
		___s_Up_5 = value;
	}

	inline static int32_t get_offset_of_s_Down_6() { return static_cast<int32_t>(offsetof(Vector3Int_tA843C5F8C2EB42492786C5AF82C3E1F4929942B4_StaticFields, ___s_Down_6)); }
	inline Vector3Int_tA843C5F8C2EB42492786C5AF82C3E1F4929942B4  get_s_Down_6() const { return ___s_Down_6; }
	inline Vector3Int_tA843C5F8C2EB42492786C5AF82C3E1F4929942B4 * get_address_of_s_Down_6() { return &___s_Down_6; }
	inline void set_s_Down_6(Vector3Int_tA843C5F8C2EB42492786C5AF82C3E1F4929942B4  value)
	{
		___s_Down_6 = value;
	}

	inline static int32_t get_offset_of_s_Left_7() { return static_cast<int32_t>(offsetof(Vector3Int_tA843C5F8C2EB42492786C5AF82C3E1F4929942B4_StaticFields, ___s_Left_7)); }
	inline Vector3Int_tA843C5F8C2EB42492786C5AF82C3E1F4929942B4  get_s_Left_7() const { return ___s_Left_7; }
	inline Vector3Int_tA843C5F8C2EB42492786C5AF82C3E1F4929942B4 * get_address_of_s_Left_7() { return &___s_Left_7; }
	inline void set_s_Left_7(Vector3Int_tA843C5F8C2EB42492786C5AF82C3E1F4929942B4  value)
	{
		___s_Left_7 = value;
	}

	inline static int32_t get_offset_of_s_Right_8() { return static_cast<int32_t>(offsetof(Vector3Int_tA843C5F8C2EB42492786C5AF82C3E1F4929942B4_StaticFields, ___s_Right_8)); }
	inline Vector3Int_tA843C5F8C2EB42492786C5AF82C3E1F4929942B4  get_s_Right_8() const { return ___s_Right_8; }
	inline Vector3Int_tA843C5F8C2EB42492786C5AF82C3E1F4929942B4 * get_address_of_s_Right_8() { return &___s_Right_8; }
	inline void set_s_Right_8(Vector3Int_tA843C5F8C2EB42492786C5AF82C3E1F4929942B4  value)
	{
		___s_Right_8 = value;
	}
};


// UnityEngine.Vector4
struct  Vector4_tD148D6428C3F8FF6CD998F82090113C2B490B76E 
{
public:
	// System.Single UnityEngine.Vector4::x
	float ___x_1;
	// System.Single UnityEngine.Vector4::y
	float ___y_2;
	// System.Single UnityEngine.Vector4::z
	float ___z_3;
	// System.Single UnityEngine.Vector4::w
	float ___w_4;

public:
	inline static int32_t get_offset_of_x_1() { return static_cast<int32_t>(offsetof(Vector4_tD148D6428C3F8FF6CD998F82090113C2B490B76E, ___x_1)); }
	inline float get_x_1() const { return ___x_1; }
	inline float* get_address_of_x_1() { return &___x_1; }
	inline void set_x_1(float value)
	{
		___x_1 = value;
	}

	inline static int32_t get_offset_of_y_2() { return static_cast<int32_t>(offsetof(Vector4_tD148D6428C3F8FF6CD998F82090113C2B490B76E, ___y_2)); }
	inline float get_y_2() const { return ___y_2; }
	inline float* get_address_of_y_2() { return &___y_2; }
	inline void set_y_2(float value)
	{
		___y_2 = value;
	}

	inline static int32_t get_offset_of_z_3() { return static_cast<int32_t>(offsetof(Vector4_tD148D6428C3F8FF6CD998F82090113C2B490B76E, ___z_3)); }
	inline float get_z_3() const { return ___z_3; }
	inline float* get_address_of_z_3() { return &___z_3; }
	inline void set_z_3(float value)
	{
		___z_3 = value;
	}

	inline static int32_t get_offset_of_w_4() { return static_cast<int32_t>(offsetof(Vector4_tD148D6428C3F8FF6CD998F82090113C2B490B76E, ___w_4)); }
	inline float get_w_4() const { return ___w_4; }
	inline float* get_address_of_w_4() { return &___w_4; }
	inline void set_w_4(float value)
	{
		___w_4 = value;
	}
};

struct Vector4_tD148D6428C3F8FF6CD998F82090113C2B490B76E_StaticFields
{
public:
	// UnityEngine.Vector4 UnityEngine.Vector4::zeroVector
	Vector4_tD148D6428C3F8FF6CD998F82090113C2B490B76E  ___zeroVector_5;
	// UnityEngine.Vector4 UnityEngine.Vector4::oneVector
	Vector4_tD148D6428C3F8FF6CD998F82090113C2B490B76E  ___oneVector_6;
	// UnityEngine.Vector4 UnityEngine.Vector4::positiveInfinityVector
	Vector4_tD148D6428C3F8FF6CD998F82090113C2B490B76E  ___positiveInfinityVector_7;
	// UnityEngine.Vector4 UnityEngine.Vector4::negativeInfinityVector
	Vector4_tD148D6428C3F8FF6CD998F82090113C2B490B76E  ___negativeInfinityVector_8;

public:
	inline static int32_t get_offset_of_zeroVector_5() { return static_cast<int32_t>(offsetof(Vector4_tD148D6428C3F8FF6CD998F82090113C2B490B76E_StaticFields, ___zeroVector_5)); }
	inline Vector4_tD148D6428C3F8FF6CD998F82090113C2B490B76E  get_zeroVector_5() const { return ___zeroVector_5; }
	inline Vector4_tD148D6428C3F8FF6CD998F82090113C2B490B76E * get_address_of_zeroVector_5() { return &___zeroVector_5; }
	inline void set_zeroVector_5(Vector4_tD148D6428C3F8FF6CD998F82090113C2B490B76E  value)
	{
		___zeroVector_5 = value;
	}

	inline static int32_t get_offset_of_oneVector_6() { return static_cast<int32_t>(offsetof(Vector4_tD148D6428C3F8FF6CD998F82090113C2B490B76E_StaticFields, ___oneVector_6)); }
	inline Vector4_tD148D6428C3F8FF6CD998F82090113C2B490B76E  get_oneVector_6() const { return ___oneVector_6; }
	inline Vector4_tD148D6428C3F8FF6CD998F82090113C2B490B76E * get_address_of_oneVector_6() { return &___oneVector_6; }
	inline void set_oneVector_6(Vector4_tD148D6428C3F8FF6CD998F82090113C2B490B76E  value)
	{
		___oneVector_6 = value;
	}

	inline static int32_t get_offset_of_positiveInfinityVector_7() { return static_cast<int32_t>(offsetof(Vector4_tD148D6428C3F8FF6CD998F82090113C2B490B76E_StaticFields, ___positiveInfinityVector_7)); }
	inline Vector4_tD148D6428C3F8FF6CD998F82090113C2B490B76E  get_positiveInfinityVector_7() const { return ___positiveInfinityVector_7; }
	inline Vector4_tD148D6428C3F8FF6CD998F82090113C2B490B76E * get_address_of_positiveInfinityVector_7() { return &___positiveInfinityVector_7; }
	inline void set_positiveInfinityVector_7(Vector4_tD148D6428C3F8FF6CD998F82090113C2B490B76E  value)
	{
		___positiveInfinityVector_7 = value;
	}

	inline static int32_t get_offset_of_negativeInfinityVector_8() { return static_cast<int32_t>(offsetof(Vector4_tD148D6428C3F8FF6CD998F82090113C2B490B76E_StaticFields, ___negativeInfinityVector_8)); }
	inline Vector4_tD148D6428C3F8FF6CD998F82090113C2B490B76E  get_negativeInfinityVector_8() const { return ___negativeInfinityVector_8; }
	inline Vector4_tD148D6428C3F8FF6CD998F82090113C2B490B76E * get_address_of_negativeInfinityVector_8() { return &___negativeInfinityVector_8; }
	inline void set_negativeInfinityVector_8(Vector4_tD148D6428C3F8FF6CD998F82090113C2B490B76E  value)
	{
		___negativeInfinityVector_8 = value;
	}
};


// UnityEngine.Yoga.YogaSize
struct  YogaSize_t0F2077727A4CBD4C36F3DC0CBE1FB0A67D9EAD23 
{
public:
	// System.Single UnityEngine.Yoga.YogaSize::width
	float ___width_0;
	// System.Single UnityEngine.Yoga.YogaSize::height
	float ___height_1;

public:
	inline static int32_t get_offset_of_width_0() { return static_cast<int32_t>(offsetof(YogaSize_t0F2077727A4CBD4C36F3DC0CBE1FB0A67D9EAD23, ___width_0)); }
	inline float get_width_0() const { return ___width_0; }
	inline float* get_address_of_width_0() { return &___width_0; }
	inline void set_width_0(float value)
	{
		___width_0 = value;
	}

	inline static int32_t get_offset_of_height_1() { return static_cast<int32_t>(offsetof(YogaSize_t0F2077727A4CBD4C36F3DC0CBE1FB0A67D9EAD23, ___height_1)); }
	inline float get_height_1() const { return ___height_1; }
	inline float* get_address_of_height_1() { return &___height_1; }
	inline void set_height_1(float value)
	{
		___height_1 = value;
	}
};


// Mirror.SyncList`1_Operation<System.Int32Enum>
struct  Operation_tD3C6912EEA79AA78D8CB3F69DD81FBE2BF26F619 
{
public:
	// System.Byte Mirror.SyncList`1_Operation::value__
	uint8_t ___value___2;

public:
	inline static int32_t get_offset_of_value___2() { return static_cast<int32_t>(offsetof(Operation_tD3C6912EEA79AA78D8CB3F69DD81FBE2BF26F619, ___value___2)); }
	inline uint8_t get_value___2() const { return ___value___2; }
	inline uint8_t* get_address_of_value___2() { return &___value___2; }
	inline void set_value___2(uint8_t value)
	{
		___value___2 = value;
	}
};


// Mirror.SyncList`1_Operation<System.Object>
struct  Operation_tD920A1EF05FC8E97391A503F92E76840886DAFE1 
{
public:
	// System.Byte Mirror.SyncList`1_Operation::value__
	uint8_t ___value___2;

public:
	inline static int32_t get_offset_of_value___2() { return static_cast<int32_t>(offsetof(Operation_tD920A1EF05FC8E97391A503F92E76840886DAFE1, ___value___2)); }
	inline uint8_t get_value___2() const { return ___value___2; }
	inline uint8_t* get_address_of_value___2() { return &___value___2; }
	inline void set_value___2(uint8_t value)
	{
		___value___2 = value;
	}
};


// Mono.Unity.UnityTls_unitytls_error_code
struct  unitytls_error_code_tA9272A71D51F7FE555C03185AE244FB01EAF998F 
{
public:
	// System.UInt32 Mono.Unity.UnityTls_unitytls_error_code::value__
	uint32_t ___value___2;

public:
	inline static int32_t get_offset_of_value___2() { return static_cast<int32_t>(offsetof(unitytls_error_code_tA9272A71D51F7FE555C03185AE244FB01EAF998F, ___value___2)); }
	inline uint32_t get_value___2() const { return ___value___2; }
	inline uint32_t* get_address_of_value___2() { return &___value___2; }
	inline void set_value___2(uint32_t value)
	{
		___value___2 = value;
	}
};


// Mono.Unity.UnityTls_unitytls_protocol
struct  unitytls_protocol_t4E7BF0D3D575EDD09EA7BF6593AAB40BBAC60DF8 
{
public:
	// System.UInt32 Mono.Unity.UnityTls_unitytls_protocol::value__
	uint32_t ___value___2;

public:
	inline static int32_t get_offset_of_value___2() { return static_cast<int32_t>(offsetof(unitytls_protocol_t4E7BF0D3D575EDD09EA7BF6593AAB40BBAC60DF8, ___value___2)); }
	inline uint32_t get_value___2() const { return ___value___2; }
	inline uint32_t* get_address_of_value___2() { return &___value___2; }
	inline void set_value___2(uint32_t value)
	{
		___value___2 = value;
	}
};


// System.Collections.Generic.KeyValuePair`2<System.DateTime,System.Object>
struct  KeyValuePair_2_t5DDBBB9A3C8CBE3A4A39721E8F0A10AEBF13737B 
{
public:
	// TKey System.Collections.Generic.KeyValuePair`2::key
	DateTime_t349B7449FBAAFF4192636E2B7A07694DA9236132  ___key_0;
	// TValue System.Collections.Generic.KeyValuePair`2::value
	RuntimeObject * ___value_1;

public:
	inline static int32_t get_offset_of_key_0() { return static_cast<int32_t>(offsetof(KeyValuePair_2_t5DDBBB9A3C8CBE3A4A39721E8F0A10AEBF13737B, ___key_0)); }
	inline DateTime_t349B7449FBAAFF4192636E2B7A07694DA9236132  get_key_0() const { return ___key_0; }
	inline DateTime_t349B7449FBAAFF4192636E2B7A07694DA9236132 * get_address_of_key_0() { return &___key_0; }
	inline void set_key_0(DateTime_t349B7449FBAAFF4192636E2B7A07694DA9236132  value)
	{
		___key_0 = value;
	}

	inline static int32_t get_offset_of_value_1() { return static_cast<int32_t>(offsetof(KeyValuePair_2_t5DDBBB9A3C8CBE3A4A39721E8F0A10AEBF13737B, ___value_1)); }
	inline RuntimeObject * get_value_1() const { return ___value_1; }
	inline RuntimeObject ** get_address_of_value_1() { return &___value_1; }
	inline void set_value_1(RuntimeObject * value)
	{
		___value_1 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___value_1), (void*)value);
	}
};


// System.ConsoleKey
struct  ConsoleKey_t0196714F06D59B40580F7B85EA2663D81394682C 
{
public:
	// System.Int32 System.ConsoleKey::value__
	int32_t ___value___2;

public:
	inline static int32_t get_offset_of_value___2() { return static_cast<int32_t>(offsetof(ConsoleKey_t0196714F06D59B40580F7B85EA2663D81394682C, ___value___2)); }
	inline int32_t get_value___2() const { return ___value___2; }
	inline int32_t* get_address_of_value___2() { return &___value___2; }
	inline void set_value___2(int32_t value)
	{
		___value___2 = value;
	}
};


// System.ConsoleModifiers
struct  ConsoleModifiers_tCB55098B71E4DCCEE972B1056E64D1B8AB9EAB34 
{
public:
	// System.Int32 System.ConsoleModifiers::value__
	int32_t ___value___2;

public:
	inline static int32_t get_offset_of_value___2() { return static_cast<int32_t>(offsetof(ConsoleModifiers_tCB55098B71E4DCCEE972B1056E64D1B8AB9EAB34, ___value___2)); }
	inline int32_t get_value___2() const { return ___value___2; }
	inline int32_t* get_address_of_value___2() { return &___value___2; }
	inline void set_value___2(int32_t value)
	{
		___value___2 = value;
	}
};


// System.Int32Enum
struct  Int32Enum_t6312CE4586C17FE2E2E513D2E7655B574F10FDCD 
{
public:
	// System.Int32 System.Int32Enum::value__
	int32_t ___value___2;

public:
	inline static int32_t get_offset_of_value___2() { return static_cast<int32_t>(offsetof(Int32Enum_t6312CE4586C17FE2E2E513D2E7655B574F10FDCD, ___value___2)); }
	inline int32_t get_value___2() const { return ___value___2; }
	inline int32_t* get_address_of_value___2() { return &___value___2; }
	inline void set_value___2(int32_t value)
	{
		___value___2 = value;
	}
};


// System.Reflection.CustomAttributeNamedArgument
struct  CustomAttributeNamedArgument_t08BA731A94FD7F173551DF3098384CB9B3056E9E 
{
public:
	// System.Reflection.CustomAttributeTypedArgument System.Reflection.CustomAttributeNamedArgument::typedArgument
	CustomAttributeTypedArgument_t238ACCB3A438CB5EDE4A924C637B288CCEC958E8  ___typedArgument_0;
	// System.Reflection.MemberInfo System.Reflection.CustomAttributeNamedArgument::memberInfo
	MemberInfo_t * ___memberInfo_1;

public:
	inline static int32_t get_offset_of_typedArgument_0() { return static_cast<int32_t>(offsetof(CustomAttributeNamedArgument_t08BA731A94FD7F173551DF3098384CB9B3056E9E, ___typedArgument_0)); }
	inline CustomAttributeTypedArgument_t238ACCB3A438CB5EDE4A924C637B288CCEC958E8  get_typedArgument_0() const { return ___typedArgument_0; }
	inline CustomAttributeTypedArgument_t238ACCB3A438CB5EDE4A924C637B288CCEC958E8 * get_address_of_typedArgument_0() { return &___typedArgument_0; }
	inline void set_typedArgument_0(CustomAttributeTypedArgument_t238ACCB3A438CB5EDE4A924C637B288CCEC958E8  value)
	{
		___typedArgument_0 = value;
		Il2CppCodeGenWriteBarrier((void**)&(((&___typedArgument_0))->___argumentType_0), (void*)NULL);
		#if IL2CPP_ENABLE_STRICT_WRITE_BARRIERS
		Il2CppCodeGenWriteBarrier((void**)&(((&___typedArgument_0))->___value_1), (void*)NULL);
		#endif
	}

	inline static int32_t get_offset_of_memberInfo_1() { return static_cast<int32_t>(offsetof(CustomAttributeNamedArgument_t08BA731A94FD7F173551DF3098384CB9B3056E9E, ___memberInfo_1)); }
	inline MemberInfo_t * get_memberInfo_1() const { return ___memberInfo_1; }
	inline MemberInfo_t ** get_address_of_memberInfo_1() { return &___memberInfo_1; }
	inline void set_memberInfo_1(MemberInfo_t * value)
	{
		___memberInfo_1 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___memberInfo_1), (void*)value);
	}
};

// Native definition for P/Invoke marshalling of System.Reflection.CustomAttributeNamedArgument
struct CustomAttributeNamedArgument_t08BA731A94FD7F173551DF3098384CB9B3056E9E_marshaled_pinvoke
{
	CustomAttributeTypedArgument_t238ACCB3A438CB5EDE4A924C637B288CCEC958E8_marshaled_pinvoke ___typedArgument_0;
	MemberInfo_t * ___memberInfo_1;
};
// Native definition for COM marshalling of System.Reflection.CustomAttributeNamedArgument
struct CustomAttributeNamedArgument_t08BA731A94FD7F173551DF3098384CB9B3056E9E_marshaled_com
{
	CustomAttributeTypedArgument_t238ACCB3A438CB5EDE4A924C637B288CCEC958E8_marshaled_com ___typedArgument_0;
	MemberInfo_t * ___memberInfo_1;
};

// System.Runtime.Serialization.StreamingContextStates
struct  StreamingContextStates_t6D16CD7BC584A66A29B702F5FD59DF62BB1BDD3F 
{
public:
	// System.Int32 System.Runtime.Serialization.StreamingContextStates::value__
	int32_t ___value___2;

public:
	inline static int32_t get_offset_of_value___2() { return static_cast<int32_t>(offsetof(StreamingContextStates_t6D16CD7BC584A66A29B702F5FD59DF62BB1BDD3F, ___value___2)); }
	inline int32_t get_value___2() const { return ___value___2; }
	inline int32_t* get_address_of_value___2() { return &___value___2; }
	inline void set_value___2(int32_t value)
	{
		___value___2 = value;
	}
};


// System.RuntimeFieldHandle
struct  RuntimeFieldHandle_t844BDF00E8E6FE69D9AEAA7657F09018B864F4EF 
{
public:
	// System.IntPtr System.RuntimeFieldHandle::value
	intptr_t ___value_0;

public:
	inline static int32_t get_offset_of_value_0() { return static_cast<int32_t>(offsetof(RuntimeFieldHandle_t844BDF00E8E6FE69D9AEAA7657F09018B864F4EF, ___value_0)); }
	inline intptr_t get_value_0() const { return ___value_0; }
	inline intptr_t* get_address_of_value_0() { return &___value_0; }
	inline void set_value_0(intptr_t value)
	{
		___value_0 = value;
	}
};


// System.RuntimeMethodHandle
struct  RuntimeMethodHandle_t85058E06EFF8AE085FAB91CE2B9E28E7F6FAE33F 
{
public:
	// System.IntPtr System.RuntimeMethodHandle::value
	intptr_t ___value_0;

public:
	inline static int32_t get_offset_of_value_0() { return static_cast<int32_t>(offsetof(RuntimeMethodHandle_t85058E06EFF8AE085FAB91CE2B9E28E7F6FAE33F, ___value_0)); }
	inline intptr_t get_value_0() const { return ___value_0; }
	inline intptr_t* get_address_of_value_0() { return &___value_0; }
	inline void set_value_0(intptr_t value)
	{
		___value_0 = value;
	}
};


// System.RuntimeTypeHandle
struct  RuntimeTypeHandle_t7B542280A22F0EC4EAC2061C29178845847A8B2D 
{
public:
	// System.IntPtr System.RuntimeTypeHandle::value
	intptr_t ___value_0;

public:
	inline static int32_t get_offset_of_value_0() { return static_cast<int32_t>(offsetof(RuntimeTypeHandle_t7B542280A22F0EC4EAC2061C29178845847A8B2D, ___value_0)); }
	inline intptr_t get_value_0() const { return ___value_0; }
	inline intptr_t* get_address_of_value_0() { return &___value_0; }
	inline void set_value_0(intptr_t value)
	{
		___value_0 = value;
	}
};


// System.TimeSpan
struct  TimeSpan_tA8069278ACE8A74D6DF7D514A9CD4432433F64C4 
{
public:
	// System.Int64 System.TimeSpan::_ticks
	int64_t ____ticks_3;

public:
	inline static int32_t get_offset_of__ticks_3() { return static_cast<int32_t>(offsetof(TimeSpan_tA8069278ACE8A74D6DF7D514A9CD4432433F64C4, ____ticks_3)); }
	inline int64_t get__ticks_3() const { return ____ticks_3; }
	inline int64_t* get_address_of__ticks_3() { return &____ticks_3; }
	inline void set__ticks_3(int64_t value)
	{
		____ticks_3 = value;
	}
};

struct TimeSpan_tA8069278ACE8A74D6DF7D514A9CD4432433F64C4_StaticFields
{
public:
	// System.TimeSpan System.TimeSpan::Zero
	TimeSpan_tA8069278ACE8A74D6DF7D514A9CD4432433F64C4  ___Zero_0;
	// System.TimeSpan System.TimeSpan::MaxValue
	TimeSpan_tA8069278ACE8A74D6DF7D514A9CD4432433F64C4  ___MaxValue_1;
	// System.TimeSpan System.TimeSpan::MinValue
	TimeSpan_tA8069278ACE8A74D6DF7D514A9CD4432433F64C4  ___MinValue_2;
	// System.Boolean modreq(System.Runtime.CompilerServices.IsVolatile) System.TimeSpan::_legacyConfigChecked
	bool ____legacyConfigChecked_4;
	// System.Boolean modreq(System.Runtime.CompilerServices.IsVolatile) System.TimeSpan::_legacyMode
	bool ____legacyMode_5;

public:
	inline static int32_t get_offset_of_Zero_0() { return static_cast<int32_t>(offsetof(TimeSpan_tA8069278ACE8A74D6DF7D514A9CD4432433F64C4_StaticFields, ___Zero_0)); }
	inline TimeSpan_tA8069278ACE8A74D6DF7D514A9CD4432433F64C4  get_Zero_0() const { return ___Zero_0; }
	inline TimeSpan_tA8069278ACE8A74D6DF7D514A9CD4432433F64C4 * get_address_of_Zero_0() { return &___Zero_0; }
	inline void set_Zero_0(TimeSpan_tA8069278ACE8A74D6DF7D514A9CD4432433F64C4  value)
	{
		___Zero_0 = value;
	}

	inline static int32_t get_offset_of_MaxValue_1() { return static_cast<int32_t>(offsetof(TimeSpan_tA8069278ACE8A74D6DF7D514A9CD4432433F64C4_StaticFields, ___MaxValue_1)); }
	inline TimeSpan_tA8069278ACE8A74D6DF7D514A9CD4432433F64C4  get_MaxValue_1() const { return ___MaxValue_1; }
	inline TimeSpan_tA8069278ACE8A74D6DF7D514A9CD4432433F64C4 * get_address_of_MaxValue_1() { return &___MaxValue_1; }
	inline void set_MaxValue_1(TimeSpan_tA8069278ACE8A74D6DF7D514A9CD4432433F64C4  value)
	{
		___MaxValue_1 = value;
	}

	inline static int32_t get_offset_of_MinValue_2() { return static_cast<int32_t>(offsetof(TimeSpan_tA8069278ACE8A74D6DF7D514A9CD4432433F64C4_StaticFields, ___MinValue_2)); }
	inline TimeSpan_tA8069278ACE8A74D6DF7D514A9CD4432433F64C4  get_MinValue_2() const { return ___MinValue_2; }
	inline TimeSpan_tA8069278ACE8A74D6DF7D514A9CD4432433F64C4 * get_address_of_MinValue_2() { return &___MinValue_2; }
	inline void set_MinValue_2(TimeSpan_tA8069278ACE8A74D6DF7D514A9CD4432433F64C4  value)
	{
		___MinValue_2 = value;
	}

	inline static int32_t get_offset_of__legacyConfigChecked_4() { return static_cast<int32_t>(offsetof(TimeSpan_tA8069278ACE8A74D6DF7D514A9CD4432433F64C4_StaticFields, ____legacyConfigChecked_4)); }
	inline bool get__legacyConfigChecked_4() const { return ____legacyConfigChecked_4; }
	inline bool* get_address_of__legacyConfigChecked_4() { return &____legacyConfigChecked_4; }
	inline void set__legacyConfigChecked_4(bool value)
	{
		____legacyConfigChecked_4 = value;
	}

	inline static int32_t get_offset_of__legacyMode_5() { return static_cast<int32_t>(offsetof(TimeSpan_tA8069278ACE8A74D6DF7D514A9CD4432433F64C4_StaticFields, ____legacyMode_5)); }
	inline bool get__legacyMode_5() const { return ____legacyMode_5; }
	inline bool* get_address_of__legacyMode_5() { return &____legacyMode_5; }
	inline void set__legacyMode_5(bool value)
	{
		____legacyMode_5 = value;
	}
};


// Telepathy.EventType
struct  EventType_t197446B222553E6B24A3952C2231914C4D043CE6 
{
public:
	// System.Int32 Telepathy.EventType::value__
	int32_t ___value___2;

public:
	inline static int32_t get_offset_of_value___2() { return static_cast<int32_t>(offsetof(EventType_t197446B222553E6B24A3952C2231914C4D043CE6, ___value___2)); }
	inline int32_t get_value___2() const { return ___value___2; }
	inline int32_t* get_address_of_value___2() { return &___value___2; }
	inline void set_value___2(int32_t value)
	{
		___value___2 = value;
	}
};


// Unity.Collections.Allocator
struct  Allocator_t62A091275262E7067EAAD565B67764FA877D58D6 
{
public:
	// System.Int32 Unity.Collections.Allocator::value__
	int32_t ___value___2;

public:
	inline static int32_t get_offset_of_value___2() { return static_cast<int32_t>(offsetof(Allocator_t62A091275262E7067EAAD565B67764FA877D58D6, ___value___2)); }
	inline int32_t get_value___2() const { return ___value___2; }
	inline int32_t* get_address_of_value___2() { return &___value___2; }
	inline void set_value___2(int32_t value)
	{
		___value___2 = value;
	}
};


// Unity.Jobs.JobHandle
struct  JobHandle_tDA498A2E49AEDE014468F416A8A98A6B258D73D1 
{
public:
	// System.IntPtr Unity.Jobs.JobHandle::jobGroup
	intptr_t ___jobGroup_0;
	// System.Int32 Unity.Jobs.JobHandle::version
	int32_t ___version_1;

public:
	inline static int32_t get_offset_of_jobGroup_0() { return static_cast<int32_t>(offsetof(JobHandle_tDA498A2E49AEDE014468F416A8A98A6B258D73D1, ___jobGroup_0)); }
	inline intptr_t get_jobGroup_0() const { return ___jobGroup_0; }
	inline intptr_t* get_address_of_jobGroup_0() { return &___jobGroup_0; }
	inline void set_jobGroup_0(intptr_t value)
	{
		___jobGroup_0 = value;
	}

	inline static int32_t get_offset_of_version_1() { return static_cast<int32_t>(offsetof(JobHandle_tDA498A2E49AEDE014468F416A8A98A6B258D73D1, ___version_1)); }
	inline int32_t get_version_1() const { return ___version_1; }
	inline int32_t* get_address_of_version_1() { return &___version_1; }
	inline void set_version_1(int32_t value)
	{
		___version_1 = value;
	}
};


// UnityEngine.CubemapFace
struct  CubemapFace_t74DD9C86D8A5E5F782F136F8753580668F96FFB9 
{
public:
	// System.Int32 UnityEngine.CubemapFace::value__
	int32_t ___value___2;

public:
	inline static int32_t get_offset_of_value___2() { return static_cast<int32_t>(offsetof(CubemapFace_t74DD9C86D8A5E5F782F136F8753580668F96FFB9, ___value___2)); }
	inline int32_t get_value___2() const { return ___value___2; }
	inline int32_t* get_address_of_value___2() { return &___value___2; }
	inline void set_value___2(int32_t value)
	{
		___value___2 = value;
	}
};


// UnityEngine.EventSystems.RaycastResult
struct  RaycastResult_t991BCED43A91EDD8580F39631DA07B1F88C58B91 
{
public:
	// UnityEngine.GameObject UnityEngine.EventSystems.RaycastResult::m_GameObject
	GameObject_tBD1244AD56B4E59AAD76E5E7C9282EC5CE434F0F * ___m_GameObject_0;
	// UnityEngine.EventSystems.BaseRaycaster UnityEngine.EventSystems.RaycastResult::module
	BaseRaycaster_tC7F6105A89F54A38FBFC2659901855FDBB0E3966 * ___module_1;
	// System.Single UnityEngine.EventSystems.RaycastResult::distance
	float ___distance_2;
	// System.Single UnityEngine.EventSystems.RaycastResult::index
	float ___index_3;
	// System.Int32 UnityEngine.EventSystems.RaycastResult::depth
	int32_t ___depth_4;
	// System.Int32 UnityEngine.EventSystems.RaycastResult::sortingLayer
	int32_t ___sortingLayer_5;
	// System.Int32 UnityEngine.EventSystems.RaycastResult::sortingOrder
	int32_t ___sortingOrder_6;
	// UnityEngine.Vector3 UnityEngine.EventSystems.RaycastResult::worldPosition
	Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  ___worldPosition_7;
	// UnityEngine.Vector3 UnityEngine.EventSystems.RaycastResult::worldNormal
	Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  ___worldNormal_8;
	// UnityEngine.Vector2 UnityEngine.EventSystems.RaycastResult::screenPosition
	Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  ___screenPosition_9;

public:
	inline static int32_t get_offset_of_m_GameObject_0() { return static_cast<int32_t>(offsetof(RaycastResult_t991BCED43A91EDD8580F39631DA07B1F88C58B91, ___m_GameObject_0)); }
	inline GameObject_tBD1244AD56B4E59AAD76E5E7C9282EC5CE434F0F * get_m_GameObject_0() const { return ___m_GameObject_0; }
	inline GameObject_tBD1244AD56B4E59AAD76E5E7C9282EC5CE434F0F ** get_address_of_m_GameObject_0() { return &___m_GameObject_0; }
	inline void set_m_GameObject_0(GameObject_tBD1244AD56B4E59AAD76E5E7C9282EC5CE434F0F * value)
	{
		___m_GameObject_0 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___m_GameObject_0), (void*)value);
	}

	inline static int32_t get_offset_of_module_1() { return static_cast<int32_t>(offsetof(RaycastResult_t991BCED43A91EDD8580F39631DA07B1F88C58B91, ___module_1)); }
	inline BaseRaycaster_tC7F6105A89F54A38FBFC2659901855FDBB0E3966 * get_module_1() const { return ___module_1; }
	inline BaseRaycaster_tC7F6105A89F54A38FBFC2659901855FDBB0E3966 ** get_address_of_module_1() { return &___module_1; }
	inline void set_module_1(BaseRaycaster_tC7F6105A89F54A38FBFC2659901855FDBB0E3966 * value)
	{
		___module_1 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___module_1), (void*)value);
	}

	inline static int32_t get_offset_of_distance_2() { return static_cast<int32_t>(offsetof(RaycastResult_t991BCED43A91EDD8580F39631DA07B1F88C58B91, ___distance_2)); }
	inline float get_distance_2() const { return ___distance_2; }
	inline float* get_address_of_distance_2() { return &___distance_2; }
	inline void set_distance_2(float value)
	{
		___distance_2 = value;
	}

	inline static int32_t get_offset_of_index_3() { return static_cast<int32_t>(offsetof(RaycastResult_t991BCED43A91EDD8580F39631DA07B1F88C58B91, ___index_3)); }
	inline float get_index_3() const { return ___index_3; }
	inline float* get_address_of_index_3() { return &___index_3; }
	inline void set_index_3(float value)
	{
		___index_3 = value;
	}

	inline static int32_t get_offset_of_depth_4() { return static_cast<int32_t>(offsetof(RaycastResult_t991BCED43A91EDD8580F39631DA07B1F88C58B91, ___depth_4)); }
	inline int32_t get_depth_4() const { return ___depth_4; }
	inline int32_t* get_address_of_depth_4() { return &___depth_4; }
	inline void set_depth_4(int32_t value)
	{
		___depth_4 = value;
	}

	inline static int32_t get_offset_of_sortingLayer_5() { return static_cast<int32_t>(offsetof(RaycastResult_t991BCED43A91EDD8580F39631DA07B1F88C58B91, ___sortingLayer_5)); }
	inline int32_t get_sortingLayer_5() const { return ___sortingLayer_5; }
	inline int32_t* get_address_of_sortingLayer_5() { return &___sortingLayer_5; }
	inline void set_sortingLayer_5(int32_t value)
	{
		___sortingLayer_5 = value;
	}

	inline static int32_t get_offset_of_sortingOrder_6() { return static_cast<int32_t>(offsetof(RaycastResult_t991BCED43A91EDD8580F39631DA07B1F88C58B91, ___sortingOrder_6)); }
	inline int32_t get_sortingOrder_6() const { return ___sortingOrder_6; }
	inline int32_t* get_address_of_sortingOrder_6() { return &___sortingOrder_6; }
	inline void set_sortingOrder_6(int32_t value)
	{
		___sortingOrder_6 = value;
	}

	inline static int32_t get_offset_of_worldPosition_7() { return static_cast<int32_t>(offsetof(RaycastResult_t991BCED43A91EDD8580F39631DA07B1F88C58B91, ___worldPosition_7)); }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  get_worldPosition_7() const { return ___worldPosition_7; }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720 * get_address_of_worldPosition_7() { return &___worldPosition_7; }
	inline void set_worldPosition_7(Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  value)
	{
		___worldPosition_7 = value;
	}

	inline static int32_t get_offset_of_worldNormal_8() { return static_cast<int32_t>(offsetof(RaycastResult_t991BCED43A91EDD8580F39631DA07B1F88C58B91, ___worldNormal_8)); }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  get_worldNormal_8() const { return ___worldNormal_8; }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720 * get_address_of_worldNormal_8() { return &___worldNormal_8; }
	inline void set_worldNormal_8(Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  value)
	{
		___worldNormal_8 = value;
	}

	inline static int32_t get_offset_of_screenPosition_9() { return static_cast<int32_t>(offsetof(RaycastResult_t991BCED43A91EDD8580F39631DA07B1F88C58B91, ___screenPosition_9)); }
	inline Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  get_screenPosition_9() const { return ___screenPosition_9; }
	inline Vector2_tA85D2DD88578276CA8A8796756458277E72D073D * get_address_of_screenPosition_9() { return &___screenPosition_9; }
	inline void set_screenPosition_9(Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  value)
	{
		___screenPosition_9 = value;
	}
};

// Native definition for P/Invoke marshalling of UnityEngine.EventSystems.RaycastResult
struct RaycastResult_t991BCED43A91EDD8580F39631DA07B1F88C58B91_marshaled_pinvoke
{
	GameObject_tBD1244AD56B4E59AAD76E5E7C9282EC5CE434F0F * ___m_GameObject_0;
	BaseRaycaster_tC7F6105A89F54A38FBFC2659901855FDBB0E3966 * ___module_1;
	float ___distance_2;
	float ___index_3;
	int32_t ___depth_4;
	int32_t ___sortingLayer_5;
	int32_t ___sortingOrder_6;
	Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  ___worldPosition_7;
	Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  ___worldNormal_8;
	Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  ___screenPosition_9;
};
// Native definition for COM marshalling of UnityEngine.EventSystems.RaycastResult
struct RaycastResult_t991BCED43A91EDD8580F39631DA07B1F88C58B91_marshaled_com
{
	GameObject_tBD1244AD56B4E59AAD76E5E7C9282EC5CE434F0F * ___m_GameObject_0;
	BaseRaycaster_tC7F6105A89F54A38FBFC2659901855FDBB0E3966 * ___module_1;
	float ___distance_2;
	float ___index_3;
	int32_t ___depth_4;
	int32_t ___sortingLayer_5;
	int32_t ___sortingOrder_6;
	Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  ___worldPosition_7;
	Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  ___worldNormal_8;
	Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  ___screenPosition_9;
};

// UnityEngine.Experimental.XR.GestureEventState
struct  GestureEventState_t8CD572FBB171284FB9F873ACEF77E7BDC326C922 
{
public:
	// System.UInt32 UnityEngine.Experimental.XR.GestureEventState::value__
	uint32_t ___value___2;

public:
	inline static int32_t get_offset_of_value___2() { return static_cast<int32_t>(offsetof(GestureEventState_t8CD572FBB171284FB9F873ACEF77E7BDC326C922, ___value___2)); }
	inline uint32_t get_value___2() const { return ___value___2; }
	inline uint32_t* get_address_of_value___2() { return &___value___2; }
	inline void set_value___2(uint32_t value)
	{
		___value___2 = value;
	}
};


// UnityEngine.Experimental.XR.GestureHoldValidFields
struct  GestureHoldValidFields_t6B4143D37C45FCA9780F7BADDD582B6EF0323FFE 
{
public:
	// System.UInt32 UnityEngine.Experimental.XR.GestureHoldValidFields::value__
	uint32_t ___value___2;

public:
	inline static int32_t get_offset_of_value___2() { return static_cast<int32_t>(offsetof(GestureHoldValidFields_t6B4143D37C45FCA9780F7BADDD582B6EF0323FFE, ___value___2)); }
	inline uint32_t get_value___2() const { return ___value___2; }
	inline uint32_t* get_address_of_value___2() { return &___value___2; }
	inline void set_value___2(uint32_t value)
	{
		___value___2 = value;
	}
};


// UnityEngine.Experimental.XR.GestureManipulationValidFields
struct  GestureManipulationValidFields_t38C72CBF011B56FBD9653299A864D4219ED5BC8F 
{
public:
	// System.UInt32 UnityEngine.Experimental.XR.GestureManipulationValidFields::value__
	uint32_t ___value___2;

public:
	inline static int32_t get_offset_of_value___2() { return static_cast<int32_t>(offsetof(GestureManipulationValidFields_t38C72CBF011B56FBD9653299A864D4219ED5BC8F, ___value___2)); }
	inline uint32_t get_value___2() const { return ___value___2; }
	inline uint32_t* get_address_of_value___2() { return &___value___2; }
	inline void set_value___2(uint32_t value)
	{
		___value___2 = value;
	}
};


// UnityEngine.Experimental.XR.GestureNavigationValidFields
struct  GestureNavigationValidFields_t90D7FD00C42C32C7E4CEAB7C89296AEAB80EFC7A 
{
public:
	// System.UInt32 UnityEngine.Experimental.XR.GestureNavigationValidFields::value__
	uint32_t ___value___2;

public:
	inline static int32_t get_offset_of_value___2() { return static_cast<int32_t>(offsetof(GestureNavigationValidFields_t90D7FD00C42C32C7E4CEAB7C89296AEAB80EFC7A, ___value___2)); }
	inline uint32_t get_value___2() const { return ___value___2; }
	inline uint32_t* get_address_of_value___2() { return &___value___2; }
	inline void set_value___2(uint32_t value)
	{
		___value___2 = value;
	}
};


// UnityEngine.Experimental.XR.GestureRecognitionValidFields
struct  GestureRecognitionValidFields_tB4168FF3AED1B9EB5DD9E81316827080DD500636 
{
public:
	// System.UInt32 UnityEngine.Experimental.XR.GestureRecognitionValidFields::value__
	uint32_t ___value___2;

public:
	inline static int32_t get_offset_of_value___2() { return static_cast<int32_t>(offsetof(GestureRecognitionValidFields_tB4168FF3AED1B9EB5DD9E81316827080DD500636, ___value___2)); }
	inline uint32_t get_value___2() const { return ___value___2; }
	inline uint32_t* get_address_of_value___2() { return &___value___2; }
	inline void set_value___2(uint32_t value)
	{
		___value___2 = value;
	}
};


// UnityEngine.Experimental.XR.GestureTappedValidFields
struct  GestureTappedValidFields_t47AC525F8D30838CFCAC4A8E592339A0CA7C31EB 
{
public:
	// System.UInt32 UnityEngine.Experimental.XR.GestureTappedValidFields::value__
	uint32_t ___value___2;

public:
	inline static int32_t get_offset_of_value___2() { return static_cast<int32_t>(offsetof(GestureTappedValidFields_t47AC525F8D30838CFCAC4A8E592339A0CA7C31EB, ___value___2)); }
	inline uint32_t get_value___2() const { return ___value___2; }
	inline uint32_t* get_address_of_value___2() { return &___value___2; }
	inline void set_value___2(uint32_t value)
	{
		___value___2 = value;
	}
};


// UnityEngine.Experimental.XR.GestureTrackingCoordinates
struct  GestureTrackingCoordinates_t633D971D579C2223A4A78E5A0476345C8D621C75 
{
public:
	// System.UInt32 UnityEngine.Experimental.XR.GestureTrackingCoordinates::value__
	uint32_t ___value___2;

public:
	inline static int32_t get_offset_of_value___2() { return static_cast<int32_t>(offsetof(GestureTrackingCoordinates_t633D971D579C2223A4A78E5A0476345C8D621C75, ___value___2)); }
	inline uint32_t get_value___2() const { return ___value___2; }
	inline uint32_t* get_address_of_value___2() { return &___value___2; }
	inline void set_value___2(uint32_t value)
	{
		___value___2 = value;
	}
};


// UnityEngine.Experimental.XR.MeshGenerationStatus
struct  MeshGenerationStatus_t58ABE4F39930471B888640F3BF5843A5A52CDBC6 
{
public:
	// System.Int32 UnityEngine.Experimental.XR.MeshGenerationStatus::value__
	int32_t ___value___2;

public:
	inline static int32_t get_offset_of_value___2() { return static_cast<int32_t>(offsetof(MeshGenerationStatus_t58ABE4F39930471B888640F3BF5843A5A52CDBC6, ___value___2)); }
	inline int32_t get_value___2() const { return ___value___2; }
	inline int32_t* get_address_of_value___2() { return &___value___2; }
	inline void set_value___2(int32_t value)
	{
		___value___2 = value;
	}
};


// UnityEngine.Experimental.XR.MeshVertexAttributes
struct  MeshVertexAttributes_t4C1E42BCB078C4499F890CE145589B9085A5D737 
{
public:
	// System.Int32 UnityEngine.Experimental.XR.MeshVertexAttributes::value__
	int32_t ___value___2;

public:
	inline static int32_t get_offset_of_value___2() { return static_cast<int32_t>(offsetof(MeshVertexAttributes_t4C1E42BCB078C4499F890CE145589B9085A5D737, ___value___2)); }
	inline int32_t get_value___2() const { return ___value___2; }
	inline int32_t* get_address_of_value___2() { return &___value___2; }
	inline void set_value___2(int32_t value)
	{
		___value___2 = value;
	}
};


// UnityEngine.Experimental.XR.PlaneAlignment
struct  PlaneAlignment_tA2F16C66968FD0E8F2D028575BF5A74395340AC6 
{
public:
	// System.Int32 UnityEngine.Experimental.XR.PlaneAlignment::value__
	int32_t ___value___2;

public:
	inline static int32_t get_offset_of_value___2() { return static_cast<int32_t>(offsetof(PlaneAlignment_tA2F16C66968FD0E8F2D028575BF5A74395340AC6, ___value___2)); }
	inline int32_t get_value___2() const { return ___value___2; }
	inline int32_t* get_address_of_value___2() { return &___value___2; }
	inline void set_value___2(int32_t value)
	{
		___value___2 = value;
	}
};


// UnityEngine.Experimental.XR.TrackingState
struct  TrackingState_tC867717ED982A6E61C703B6A0CCF908E9642C854 
{
public:
	// System.Int32 UnityEngine.Experimental.XR.TrackingState::value__
	int32_t ___value___2;

public:
	inline static int32_t get_offset_of_value___2() { return static_cast<int32_t>(offsetof(TrackingState_tC867717ED982A6E61C703B6A0CCF908E9642C854, ___value___2)); }
	inline int32_t get_value___2() const { return ___value___2; }
	inline int32_t* get_address_of_value___2() { return &___value___2; }
	inline void set_value___2(int32_t value)
	{
		___value___2 = value;
	}
};


// UnityEngine.Playables.PlayableGraph
struct  PlayableGraph_tEC38BBCA59BDD496F75037F220984D41339AB8BA 
{
public:
	// System.IntPtr UnityEngine.Playables.PlayableGraph::m_Handle
	intptr_t ___m_Handle_0;
	// System.UInt32 UnityEngine.Playables.PlayableGraph::m_Version
	uint32_t ___m_Version_1;

public:
	inline static int32_t get_offset_of_m_Handle_0() { return static_cast<int32_t>(offsetof(PlayableGraph_tEC38BBCA59BDD496F75037F220984D41339AB8BA, ___m_Handle_0)); }
	inline intptr_t get_m_Handle_0() const { return ___m_Handle_0; }
	inline intptr_t* get_address_of_m_Handle_0() { return &___m_Handle_0; }
	inline void set_m_Handle_0(intptr_t value)
	{
		___m_Handle_0 = value;
	}

	inline static int32_t get_offset_of_m_Version_1() { return static_cast<int32_t>(offsetof(PlayableGraph_tEC38BBCA59BDD496F75037F220984D41339AB8BA, ___m_Version_1)); }
	inline uint32_t get_m_Version_1() const { return ___m_Version_1; }
	inline uint32_t* get_address_of_m_Version_1() { return &___m_Version_1; }
	inline void set_m_Version_1(uint32_t value)
	{
		___m_Version_1 = value;
	}
};


// UnityEngine.Playables.PlayableHandle
struct  PlayableHandle_t9D3B4E540D4413CED81DDD6A24C5373BEFA1D182 
{
public:
	// System.IntPtr UnityEngine.Playables.PlayableHandle::m_Handle
	intptr_t ___m_Handle_0;
	// System.UInt32 UnityEngine.Playables.PlayableHandle::m_Version
	uint32_t ___m_Version_1;

public:
	inline static int32_t get_offset_of_m_Handle_0() { return static_cast<int32_t>(offsetof(PlayableHandle_t9D3B4E540D4413CED81DDD6A24C5373BEFA1D182, ___m_Handle_0)); }
	inline intptr_t get_m_Handle_0() const { return ___m_Handle_0; }
	inline intptr_t* get_address_of_m_Handle_0() { return &___m_Handle_0; }
	inline void set_m_Handle_0(intptr_t value)
	{
		___m_Handle_0 = value;
	}

	inline static int32_t get_offset_of_m_Version_1() { return static_cast<int32_t>(offsetof(PlayableHandle_t9D3B4E540D4413CED81DDD6A24C5373BEFA1D182, ___m_Version_1)); }
	inline uint32_t get_m_Version_1() const { return ___m_Version_1; }
	inline uint32_t* get_address_of_m_Version_1() { return &___m_Version_1; }
	inline void set_m_Version_1(uint32_t value)
	{
		___m_Version_1 = value;
	}
};

struct PlayableHandle_t9D3B4E540D4413CED81DDD6A24C5373BEFA1D182_StaticFields
{
public:
	// UnityEngine.Playables.PlayableHandle UnityEngine.Playables.PlayableHandle::m_Null
	PlayableHandle_t9D3B4E540D4413CED81DDD6A24C5373BEFA1D182  ___m_Null_2;

public:
	inline static int32_t get_offset_of_m_Null_2() { return static_cast<int32_t>(offsetof(PlayableHandle_t9D3B4E540D4413CED81DDD6A24C5373BEFA1D182_StaticFields, ___m_Null_2)); }
	inline PlayableHandle_t9D3B4E540D4413CED81DDD6A24C5373BEFA1D182  get_m_Null_2() const { return ___m_Null_2; }
	inline PlayableHandle_t9D3B4E540D4413CED81DDD6A24C5373BEFA1D182 * get_address_of_m_Null_2() { return &___m_Null_2; }
	inline void set_m_Null_2(PlayableHandle_t9D3B4E540D4413CED81DDD6A24C5373BEFA1D182  value)
	{
		___m_Null_2 = value;
	}
};


// UnityEngine.Playables.PlayableOutputHandle
struct  PlayableOutputHandle_t0D0C9D8ACC1A4061BD4EAEB61F3EE0357052F922 
{
public:
	// System.IntPtr UnityEngine.Playables.PlayableOutputHandle::m_Handle
	intptr_t ___m_Handle_0;
	// System.UInt32 UnityEngine.Playables.PlayableOutputHandle::m_Version
	uint32_t ___m_Version_1;

public:
	inline static int32_t get_offset_of_m_Handle_0() { return static_cast<int32_t>(offsetof(PlayableOutputHandle_t0D0C9D8ACC1A4061BD4EAEB61F3EE0357052F922, ___m_Handle_0)); }
	inline intptr_t get_m_Handle_0() const { return ___m_Handle_0; }
	inline intptr_t* get_address_of_m_Handle_0() { return &___m_Handle_0; }
	inline void set_m_Handle_0(intptr_t value)
	{
		___m_Handle_0 = value;
	}

	inline static int32_t get_offset_of_m_Version_1() { return static_cast<int32_t>(offsetof(PlayableOutputHandle_t0D0C9D8ACC1A4061BD4EAEB61F3EE0357052F922, ___m_Version_1)); }
	inline uint32_t get_m_Version_1() const { return ___m_Version_1; }
	inline uint32_t* get_address_of_m_Version_1() { return &___m_Version_1; }
	inline void set_m_Version_1(uint32_t value)
	{
		___m_Version_1 = value;
	}
};

struct PlayableOutputHandle_t0D0C9D8ACC1A4061BD4EAEB61F3EE0357052F922_StaticFields
{
public:
	// UnityEngine.Playables.PlayableOutputHandle UnityEngine.Playables.PlayableOutputHandle::m_Null
	PlayableOutputHandle_t0D0C9D8ACC1A4061BD4EAEB61F3EE0357052F922  ___m_Null_2;

public:
	inline static int32_t get_offset_of_m_Null_2() { return static_cast<int32_t>(offsetof(PlayableOutputHandle_t0D0C9D8ACC1A4061BD4EAEB61F3EE0357052F922_StaticFields, ___m_Null_2)); }
	inline PlayableOutputHandle_t0D0C9D8ACC1A4061BD4EAEB61F3EE0357052F922  get_m_Null_2() const { return ___m_Null_2; }
	inline PlayableOutputHandle_t0D0C9D8ACC1A4061BD4EAEB61F3EE0357052F922 * get_address_of_m_Null_2() { return &___m_Null_2; }
	inline void set_m_Null_2(PlayableOutputHandle_t0D0C9D8ACC1A4061BD4EAEB61F3EE0357052F922  value)
	{
		___m_Null_2 = value;
	}
};


// UnityEngine.Pose
struct  Pose_t2997DE3CB3863E4D78FCF42B46FC481818823F29 
{
public:
	// UnityEngine.Vector3 UnityEngine.Pose::position
	Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  ___position_0;
	// UnityEngine.Quaternion UnityEngine.Pose::rotation
	Quaternion_t319F3319A7D43FFA5D819AD6C0A98851F0095357  ___rotation_1;

public:
	inline static int32_t get_offset_of_position_0() { return static_cast<int32_t>(offsetof(Pose_t2997DE3CB3863E4D78FCF42B46FC481818823F29, ___position_0)); }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  get_position_0() const { return ___position_0; }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720 * get_address_of_position_0() { return &___position_0; }
	inline void set_position_0(Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  value)
	{
		___position_0 = value;
	}

	inline static int32_t get_offset_of_rotation_1() { return static_cast<int32_t>(offsetof(Pose_t2997DE3CB3863E4D78FCF42B46FC481818823F29, ___rotation_1)); }
	inline Quaternion_t319F3319A7D43FFA5D819AD6C0A98851F0095357  get_rotation_1() const { return ___rotation_1; }
	inline Quaternion_t319F3319A7D43FFA5D819AD6C0A98851F0095357 * get_address_of_rotation_1() { return &___rotation_1; }
	inline void set_rotation_1(Quaternion_t319F3319A7D43FFA5D819AD6C0A98851F0095357  value)
	{
		___rotation_1 = value;
	}
};

struct Pose_t2997DE3CB3863E4D78FCF42B46FC481818823F29_StaticFields
{
public:
	// UnityEngine.Pose UnityEngine.Pose::k_Identity
	Pose_t2997DE3CB3863E4D78FCF42B46FC481818823F29  ___k_Identity_2;

public:
	inline static int32_t get_offset_of_k_Identity_2() { return static_cast<int32_t>(offsetof(Pose_t2997DE3CB3863E4D78FCF42B46FC481818823F29_StaticFields, ___k_Identity_2)); }
	inline Pose_t2997DE3CB3863E4D78FCF42B46FC481818823F29  get_k_Identity_2() const { return ___k_Identity_2; }
	inline Pose_t2997DE3CB3863E4D78FCF42B46FC481818823F29 * get_address_of_k_Identity_2() { return &___k_Identity_2; }
	inline void set_k_Identity_2(Pose_t2997DE3CB3863E4D78FCF42B46FC481818823F29  value)
	{
		___k_Identity_2 = value;
	}
};


// UnityEngine.Ray
struct  Ray_tE2163D4CB3E6B267E29F8ABE41684490E4A614B2 
{
public:
	// UnityEngine.Vector3 UnityEngine.Ray::m_Origin
	Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  ___m_Origin_0;
	// UnityEngine.Vector3 UnityEngine.Ray::m_Direction
	Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  ___m_Direction_1;

public:
	inline static int32_t get_offset_of_m_Origin_0() { return static_cast<int32_t>(offsetof(Ray_tE2163D4CB3E6B267E29F8ABE41684490E4A614B2, ___m_Origin_0)); }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  get_m_Origin_0() const { return ___m_Origin_0; }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720 * get_address_of_m_Origin_0() { return &___m_Origin_0; }
	inline void set_m_Origin_0(Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  value)
	{
		___m_Origin_0 = value;
	}

	inline static int32_t get_offset_of_m_Direction_1() { return static_cast<int32_t>(offsetof(Ray_tE2163D4CB3E6B267E29F8ABE41684490E4A614B2, ___m_Direction_1)); }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  get_m_Direction_1() const { return ___m_Direction_1; }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720 * get_address_of_m_Direction_1() { return &___m_Direction_1; }
	inline void set_m_Direction_1(Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  value)
	{
		___m_Direction_1 = value;
	}
};


// UnityEngine.RaycastHit
struct  RaycastHit_t19695F18F9265FE5425062BBA6A4D330480538C3 
{
public:
	// UnityEngine.Vector3 UnityEngine.RaycastHit::m_Point
	Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  ___m_Point_0;
	// UnityEngine.Vector3 UnityEngine.RaycastHit::m_Normal
	Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  ___m_Normal_1;
	// System.UInt32 UnityEngine.RaycastHit::m_FaceID
	uint32_t ___m_FaceID_2;
	// System.Single UnityEngine.RaycastHit::m_Distance
	float ___m_Distance_3;
	// UnityEngine.Vector2 UnityEngine.RaycastHit::m_UV
	Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  ___m_UV_4;
	// System.Int32 UnityEngine.RaycastHit::m_Collider
	int32_t ___m_Collider_5;

public:
	inline static int32_t get_offset_of_m_Point_0() { return static_cast<int32_t>(offsetof(RaycastHit_t19695F18F9265FE5425062BBA6A4D330480538C3, ___m_Point_0)); }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  get_m_Point_0() const { return ___m_Point_0; }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720 * get_address_of_m_Point_0() { return &___m_Point_0; }
	inline void set_m_Point_0(Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  value)
	{
		___m_Point_0 = value;
	}

	inline static int32_t get_offset_of_m_Normal_1() { return static_cast<int32_t>(offsetof(RaycastHit_t19695F18F9265FE5425062BBA6A4D330480538C3, ___m_Normal_1)); }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  get_m_Normal_1() const { return ___m_Normal_1; }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720 * get_address_of_m_Normal_1() { return &___m_Normal_1; }
	inline void set_m_Normal_1(Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  value)
	{
		___m_Normal_1 = value;
	}

	inline static int32_t get_offset_of_m_FaceID_2() { return static_cast<int32_t>(offsetof(RaycastHit_t19695F18F9265FE5425062BBA6A4D330480538C3, ___m_FaceID_2)); }
	inline uint32_t get_m_FaceID_2() const { return ___m_FaceID_2; }
	inline uint32_t* get_address_of_m_FaceID_2() { return &___m_FaceID_2; }
	inline void set_m_FaceID_2(uint32_t value)
	{
		___m_FaceID_2 = value;
	}

	inline static int32_t get_offset_of_m_Distance_3() { return static_cast<int32_t>(offsetof(RaycastHit_t19695F18F9265FE5425062BBA6A4D330480538C3, ___m_Distance_3)); }
	inline float get_m_Distance_3() const { return ___m_Distance_3; }
	inline float* get_address_of_m_Distance_3() { return &___m_Distance_3; }
	inline void set_m_Distance_3(float value)
	{
		___m_Distance_3 = value;
	}

	inline static int32_t get_offset_of_m_UV_4() { return static_cast<int32_t>(offsetof(RaycastHit_t19695F18F9265FE5425062BBA6A4D330480538C3, ___m_UV_4)); }
	inline Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  get_m_UV_4() const { return ___m_UV_4; }
	inline Vector2_tA85D2DD88578276CA8A8796756458277E72D073D * get_address_of_m_UV_4() { return &___m_UV_4; }
	inline void set_m_UV_4(Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  value)
	{
		___m_UV_4 = value;
	}

	inline static int32_t get_offset_of_m_Collider_5() { return static_cast<int32_t>(offsetof(RaycastHit_t19695F18F9265FE5425062BBA6A4D330480538C3, ___m_Collider_5)); }
	inline int32_t get_m_Collider_5() const { return ___m_Collider_5; }
	inline int32_t* get_address_of_m_Collider_5() { return &___m_Collider_5; }
	inline void set_m_Collider_5(int32_t value)
	{
		___m_Collider_5 = value;
	}
};


// UnityEngine.RaycastHit2D
struct  RaycastHit2D_t5E8A7F96317BAF2033362FC780F4D72DC72764BE 
{
public:
	// UnityEngine.Vector2 UnityEngine.RaycastHit2D::m_Centroid
	Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  ___m_Centroid_0;
	// UnityEngine.Vector2 UnityEngine.RaycastHit2D::m_Point
	Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  ___m_Point_1;
	// UnityEngine.Vector2 UnityEngine.RaycastHit2D::m_Normal
	Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  ___m_Normal_2;
	// System.Single UnityEngine.RaycastHit2D::m_Distance
	float ___m_Distance_3;
	// System.Single UnityEngine.RaycastHit2D::m_Fraction
	float ___m_Fraction_4;
	// System.Int32 UnityEngine.RaycastHit2D::m_Collider
	int32_t ___m_Collider_5;

public:
	inline static int32_t get_offset_of_m_Centroid_0() { return static_cast<int32_t>(offsetof(RaycastHit2D_t5E8A7F96317BAF2033362FC780F4D72DC72764BE, ___m_Centroid_0)); }
	inline Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  get_m_Centroid_0() const { return ___m_Centroid_0; }
	inline Vector2_tA85D2DD88578276CA8A8796756458277E72D073D * get_address_of_m_Centroid_0() { return &___m_Centroid_0; }
	inline void set_m_Centroid_0(Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  value)
	{
		___m_Centroid_0 = value;
	}

	inline static int32_t get_offset_of_m_Point_1() { return static_cast<int32_t>(offsetof(RaycastHit2D_t5E8A7F96317BAF2033362FC780F4D72DC72764BE, ___m_Point_1)); }
	inline Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  get_m_Point_1() const { return ___m_Point_1; }
	inline Vector2_tA85D2DD88578276CA8A8796756458277E72D073D * get_address_of_m_Point_1() { return &___m_Point_1; }
	inline void set_m_Point_1(Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  value)
	{
		___m_Point_1 = value;
	}

	inline static int32_t get_offset_of_m_Normal_2() { return static_cast<int32_t>(offsetof(RaycastHit2D_t5E8A7F96317BAF2033362FC780F4D72DC72764BE, ___m_Normal_2)); }
	inline Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  get_m_Normal_2() const { return ___m_Normal_2; }
	inline Vector2_tA85D2DD88578276CA8A8796756458277E72D073D * get_address_of_m_Normal_2() { return &___m_Normal_2; }
	inline void set_m_Normal_2(Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  value)
	{
		___m_Normal_2 = value;
	}

	inline static int32_t get_offset_of_m_Distance_3() { return static_cast<int32_t>(offsetof(RaycastHit2D_t5E8A7F96317BAF2033362FC780F4D72DC72764BE, ___m_Distance_3)); }
	inline float get_m_Distance_3() const { return ___m_Distance_3; }
	inline float* get_address_of_m_Distance_3() { return &___m_Distance_3; }
	inline void set_m_Distance_3(float value)
	{
		___m_Distance_3 = value;
	}

	inline static int32_t get_offset_of_m_Fraction_4() { return static_cast<int32_t>(offsetof(RaycastHit2D_t5E8A7F96317BAF2033362FC780F4D72DC72764BE, ___m_Fraction_4)); }
	inline float get_m_Fraction_4() const { return ___m_Fraction_4; }
	inline float* get_address_of_m_Fraction_4() { return &___m_Fraction_4; }
	inline void set_m_Fraction_4(float value)
	{
		___m_Fraction_4 = value;
	}

	inline static int32_t get_offset_of_m_Collider_5() { return static_cast<int32_t>(offsetof(RaycastHit2D_t5E8A7F96317BAF2033362FC780F4D72DC72764BE, ___m_Collider_5)); }
	inline int32_t get_m_Collider_5() const { return ___m_Collider_5; }
	inline int32_t* get_address_of_m_Collider_5() { return &___m_Collider_5; }
	inline void set_m_Collider_5(int32_t value)
	{
		___m_Collider_5 = value;
	}
};


// UnityEngine.Rendering.BuiltinRenderTextureType
struct  BuiltinRenderTextureType_t6ECEE9FF62E815D7ED640D057EEA84C0FD145D01 
{
public:
	// System.Int32 UnityEngine.Rendering.BuiltinRenderTextureType::value__
	int32_t ___value___2;

public:
	inline static int32_t get_offset_of_value___2() { return static_cast<int32_t>(offsetof(BuiltinRenderTextureType_t6ECEE9FF62E815D7ED640D057EEA84C0FD145D01, ___value___2)); }
	inline int32_t get_value___2() const { return ___value___2; }
	inline int32_t* get_address_of_value___2() { return &___value___2; }
	inline void set_value___2(int32_t value)
	{
		___value___2 = value;
	}
};


// UnityEngine.Rendering.LODParameters
struct  LODParameters_t8CBE0C157487BE3E860DA9478FB46F80D3D1D960 
{
public:
	// System.Int32 UnityEngine.Rendering.LODParameters::m_IsOrthographic
	int32_t ___m_IsOrthographic_0;
	// UnityEngine.Vector3 UnityEngine.Rendering.LODParameters::m_CameraPosition
	Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  ___m_CameraPosition_1;
	// System.Single UnityEngine.Rendering.LODParameters::m_FieldOfView
	float ___m_FieldOfView_2;
	// System.Single UnityEngine.Rendering.LODParameters::m_OrthoSize
	float ___m_OrthoSize_3;
	// System.Int32 UnityEngine.Rendering.LODParameters::m_CameraPixelHeight
	int32_t ___m_CameraPixelHeight_4;

public:
	inline static int32_t get_offset_of_m_IsOrthographic_0() { return static_cast<int32_t>(offsetof(LODParameters_t8CBE0C157487BE3E860DA9478FB46F80D3D1D960, ___m_IsOrthographic_0)); }
	inline int32_t get_m_IsOrthographic_0() const { return ___m_IsOrthographic_0; }
	inline int32_t* get_address_of_m_IsOrthographic_0() { return &___m_IsOrthographic_0; }
	inline void set_m_IsOrthographic_0(int32_t value)
	{
		___m_IsOrthographic_0 = value;
	}

	inline static int32_t get_offset_of_m_CameraPosition_1() { return static_cast<int32_t>(offsetof(LODParameters_t8CBE0C157487BE3E860DA9478FB46F80D3D1D960, ___m_CameraPosition_1)); }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  get_m_CameraPosition_1() const { return ___m_CameraPosition_1; }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720 * get_address_of_m_CameraPosition_1() { return &___m_CameraPosition_1; }
	inline void set_m_CameraPosition_1(Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  value)
	{
		___m_CameraPosition_1 = value;
	}

	inline static int32_t get_offset_of_m_FieldOfView_2() { return static_cast<int32_t>(offsetof(LODParameters_t8CBE0C157487BE3E860DA9478FB46F80D3D1D960, ___m_FieldOfView_2)); }
	inline float get_m_FieldOfView_2() const { return ___m_FieldOfView_2; }
	inline float* get_address_of_m_FieldOfView_2() { return &___m_FieldOfView_2; }
	inline void set_m_FieldOfView_2(float value)
	{
		___m_FieldOfView_2 = value;
	}

	inline static int32_t get_offset_of_m_OrthoSize_3() { return static_cast<int32_t>(offsetof(LODParameters_t8CBE0C157487BE3E860DA9478FB46F80D3D1D960, ___m_OrthoSize_3)); }
	inline float get_m_OrthoSize_3() const { return ___m_OrthoSize_3; }
	inline float* get_address_of_m_OrthoSize_3() { return &___m_OrthoSize_3; }
	inline void set_m_OrthoSize_3(float value)
	{
		___m_OrthoSize_3 = value;
	}

	inline static int32_t get_offset_of_m_CameraPixelHeight_4() { return static_cast<int32_t>(offsetof(LODParameters_t8CBE0C157487BE3E860DA9478FB46F80D3D1D960, ___m_CameraPixelHeight_4)); }
	inline int32_t get_m_CameraPixelHeight_4() const { return ___m_CameraPixelHeight_4; }
	inline int32_t* get_address_of_m_CameraPixelHeight_4() { return &___m_CameraPixelHeight_4; }
	inline void set_m_CameraPixelHeight_4(int32_t value)
	{
		___m_CameraPixelHeight_4 = value;
	}
};


// UnityEngine.Rendering.ScriptableRenderContext
struct  ScriptableRenderContext_t7A3C889E3516E8C79C1C0327D33ED9601D163A2B 
{
public:
	// System.IntPtr UnityEngine.Rendering.ScriptableRenderContext::m_Ptr
	intptr_t ___m_Ptr_0;

public:
	inline static int32_t get_offset_of_m_Ptr_0() { return static_cast<int32_t>(offsetof(ScriptableRenderContext_t7A3C889E3516E8C79C1C0327D33ED9601D163A2B, ___m_Ptr_0)); }
	inline intptr_t get_m_Ptr_0() const { return ___m_Ptr_0; }
	inline intptr_t* get_address_of_m_Ptr_0() { return &___m_Ptr_0; }
	inline void set_m_Ptr_0(intptr_t value)
	{
		___m_Ptr_0 = value;
	}
};


// UnityEngine.TouchPhase
struct  TouchPhase_t7E9CEC3DD059E32F847242513BD6CE30866AB2A6 
{
public:
	// System.Int32 UnityEngine.TouchPhase::value__
	int32_t ___value___2;

public:
	inline static int32_t get_offset_of_value___2() { return static_cast<int32_t>(offsetof(TouchPhase_t7E9CEC3DD059E32F847242513BD6CE30866AB2A6, ___value___2)); }
	inline int32_t get_value___2() const { return ___value___2; }
	inline int32_t* get_address_of_value___2() { return &___value___2; }
	inline void set_value___2(int32_t value)
	{
		___value___2 = value;
	}
};


// UnityEngine.TouchType
struct  TouchType_tBBD83025576FC017B10484014B5C396613A02B8E 
{
public:
	// System.Int32 UnityEngine.TouchType::value__
	int32_t ___value___2;

public:
	inline static int32_t get_offset_of_value___2() { return static_cast<int32_t>(offsetof(TouchType_tBBD83025576FC017B10484014B5C396613A02B8E, ___value___2)); }
	inline int32_t get_value___2() const { return ___value___2; }
	inline int32_t* get_address_of_value___2() { return &___value___2; }
	inline void set_value___2(int32_t value)
	{
		___value___2 = value;
	}
};


// UnityEngine.UI.ColorBlock
struct  ColorBlock_t93B54DF6E8D65D24CEA9726CA745E48C53E3B1EA 
{
public:
	// UnityEngine.Color UnityEngine.UI.ColorBlock::m_NormalColor
	Color_t119BCA590009762C7223FDD3AF9706653AC84ED2  ___m_NormalColor_0;
	// UnityEngine.Color UnityEngine.UI.ColorBlock::m_HighlightedColor
	Color_t119BCA590009762C7223FDD3AF9706653AC84ED2  ___m_HighlightedColor_1;
	// UnityEngine.Color UnityEngine.UI.ColorBlock::m_PressedColor
	Color_t119BCA590009762C7223FDD3AF9706653AC84ED2  ___m_PressedColor_2;
	// UnityEngine.Color UnityEngine.UI.ColorBlock::m_SelectedColor
	Color_t119BCA590009762C7223FDD3AF9706653AC84ED2  ___m_SelectedColor_3;
	// UnityEngine.Color UnityEngine.UI.ColorBlock::m_DisabledColor
	Color_t119BCA590009762C7223FDD3AF9706653AC84ED2  ___m_DisabledColor_4;
	// System.Single UnityEngine.UI.ColorBlock::m_ColorMultiplier
	float ___m_ColorMultiplier_5;
	// System.Single UnityEngine.UI.ColorBlock::m_FadeDuration
	float ___m_FadeDuration_6;

public:
	inline static int32_t get_offset_of_m_NormalColor_0() { return static_cast<int32_t>(offsetof(ColorBlock_t93B54DF6E8D65D24CEA9726CA745E48C53E3B1EA, ___m_NormalColor_0)); }
	inline Color_t119BCA590009762C7223FDD3AF9706653AC84ED2  get_m_NormalColor_0() const { return ___m_NormalColor_0; }
	inline Color_t119BCA590009762C7223FDD3AF9706653AC84ED2 * get_address_of_m_NormalColor_0() { return &___m_NormalColor_0; }
	inline void set_m_NormalColor_0(Color_t119BCA590009762C7223FDD3AF9706653AC84ED2  value)
	{
		___m_NormalColor_0 = value;
	}

	inline static int32_t get_offset_of_m_HighlightedColor_1() { return static_cast<int32_t>(offsetof(ColorBlock_t93B54DF6E8D65D24CEA9726CA745E48C53E3B1EA, ___m_HighlightedColor_1)); }
	inline Color_t119BCA590009762C7223FDD3AF9706653AC84ED2  get_m_HighlightedColor_1() const { return ___m_HighlightedColor_1; }
	inline Color_t119BCA590009762C7223FDD3AF9706653AC84ED2 * get_address_of_m_HighlightedColor_1() { return &___m_HighlightedColor_1; }
	inline void set_m_HighlightedColor_1(Color_t119BCA590009762C7223FDD3AF9706653AC84ED2  value)
	{
		___m_HighlightedColor_1 = value;
	}

	inline static int32_t get_offset_of_m_PressedColor_2() { return static_cast<int32_t>(offsetof(ColorBlock_t93B54DF6E8D65D24CEA9726CA745E48C53E3B1EA, ___m_PressedColor_2)); }
	inline Color_t119BCA590009762C7223FDD3AF9706653AC84ED2  get_m_PressedColor_2() const { return ___m_PressedColor_2; }
	inline Color_t119BCA590009762C7223FDD3AF9706653AC84ED2 * get_address_of_m_PressedColor_2() { return &___m_PressedColor_2; }
	inline void set_m_PressedColor_2(Color_t119BCA590009762C7223FDD3AF9706653AC84ED2  value)
	{
		___m_PressedColor_2 = value;
	}

	inline static int32_t get_offset_of_m_SelectedColor_3() { return static_cast<int32_t>(offsetof(ColorBlock_t93B54DF6E8D65D24CEA9726CA745E48C53E3B1EA, ___m_SelectedColor_3)); }
	inline Color_t119BCA590009762C7223FDD3AF9706653AC84ED2  get_m_SelectedColor_3() const { return ___m_SelectedColor_3; }
	inline Color_t119BCA590009762C7223FDD3AF9706653AC84ED2 * get_address_of_m_SelectedColor_3() { return &___m_SelectedColor_3; }
	inline void set_m_SelectedColor_3(Color_t119BCA590009762C7223FDD3AF9706653AC84ED2  value)
	{
		___m_SelectedColor_3 = value;
	}

	inline static int32_t get_offset_of_m_DisabledColor_4() { return static_cast<int32_t>(offsetof(ColorBlock_t93B54DF6E8D65D24CEA9726CA745E48C53E3B1EA, ___m_DisabledColor_4)); }
	inline Color_t119BCA590009762C7223FDD3AF9706653AC84ED2  get_m_DisabledColor_4() const { return ___m_DisabledColor_4; }
	inline Color_t119BCA590009762C7223FDD3AF9706653AC84ED2 * get_address_of_m_DisabledColor_4() { return &___m_DisabledColor_4; }
	inline void set_m_DisabledColor_4(Color_t119BCA590009762C7223FDD3AF9706653AC84ED2  value)
	{
		___m_DisabledColor_4 = value;
	}

	inline static int32_t get_offset_of_m_ColorMultiplier_5() { return static_cast<int32_t>(offsetof(ColorBlock_t93B54DF6E8D65D24CEA9726CA745E48C53E3B1EA, ___m_ColorMultiplier_5)); }
	inline float get_m_ColorMultiplier_5() const { return ___m_ColorMultiplier_5; }
	inline float* get_address_of_m_ColorMultiplier_5() { return &___m_ColorMultiplier_5; }
	inline void set_m_ColorMultiplier_5(float value)
	{
		___m_ColorMultiplier_5 = value;
	}

	inline static int32_t get_offset_of_m_FadeDuration_6() { return static_cast<int32_t>(offsetof(ColorBlock_t93B54DF6E8D65D24CEA9726CA745E48C53E3B1EA, ___m_FadeDuration_6)); }
	inline float get_m_FadeDuration_6() const { return ___m_FadeDuration_6; }
	inline float* get_address_of_m_FadeDuration_6() { return &___m_FadeDuration_6; }
	inline void set_m_FadeDuration_6(float value)
	{
		___m_FadeDuration_6 = value;
	}
};


// UnityEngine.UI.Navigation_Mode
struct  Mode_t93F92BD50B147AE38D82BA33FA77FD247A59FE26 
{
public:
	// System.Int32 UnityEngine.UI.Navigation_Mode::value__
	int32_t ___value___2;

public:
	inline static int32_t get_offset_of_value___2() { return static_cast<int32_t>(offsetof(Mode_t93F92BD50B147AE38D82BA33FA77FD247A59FE26, ___value___2)); }
	inline int32_t get_value___2() const { return ___value___2; }
	inline int32_t* get_address_of_value___2() { return &___value___2; }
	inline void set_value___2(int32_t value)
	{
		___value___2 = value;
	}
};


// UnityEngine.UICharInfo
struct  UICharInfo_tB4C92043A686A600D36A92E3108F173C499E318A 
{
public:
	// UnityEngine.Vector2 UnityEngine.UICharInfo::cursorPos
	Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  ___cursorPos_0;
	// System.Single UnityEngine.UICharInfo::charWidth
	float ___charWidth_1;

public:
	inline static int32_t get_offset_of_cursorPos_0() { return static_cast<int32_t>(offsetof(UICharInfo_tB4C92043A686A600D36A92E3108F173C499E318A, ___cursorPos_0)); }
	inline Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  get_cursorPos_0() const { return ___cursorPos_0; }
	inline Vector2_tA85D2DD88578276CA8A8796756458277E72D073D * get_address_of_cursorPos_0() { return &___cursorPos_0; }
	inline void set_cursorPos_0(Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  value)
	{
		___cursorPos_0 = value;
	}

	inline static int32_t get_offset_of_charWidth_1() { return static_cast<int32_t>(offsetof(UICharInfo_tB4C92043A686A600D36A92E3108F173C499E318A, ___charWidth_1)); }
	inline float get_charWidth_1() const { return ___charWidth_1; }
	inline float* get_address_of_charWidth_1() { return &___charWidth_1; }
	inline void set_charWidth_1(float value)
	{
		___charWidth_1 = value;
	}
};


// UnityEngine.UIVertex
struct  UIVertex_t0583C35B730B218B542E80203F5F4BC6F1E9E577 
{
public:
	// UnityEngine.Vector3 UnityEngine.UIVertex::position
	Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  ___position_0;
	// UnityEngine.Vector3 UnityEngine.UIVertex::normal
	Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  ___normal_1;
	// UnityEngine.Vector4 UnityEngine.UIVertex::tangent
	Vector4_tD148D6428C3F8FF6CD998F82090113C2B490B76E  ___tangent_2;
	// UnityEngine.Color32 UnityEngine.UIVertex::color
	Color32_t23ABC4AE0E0BDFD2E22EE1FA0DA3904FFE5F6E23  ___color_3;
	// UnityEngine.Vector2 UnityEngine.UIVertex::uv0
	Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  ___uv0_4;
	// UnityEngine.Vector2 UnityEngine.UIVertex::uv1
	Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  ___uv1_5;
	// UnityEngine.Vector2 UnityEngine.UIVertex::uv2
	Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  ___uv2_6;
	// UnityEngine.Vector2 UnityEngine.UIVertex::uv3
	Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  ___uv3_7;

public:
	inline static int32_t get_offset_of_position_0() { return static_cast<int32_t>(offsetof(UIVertex_t0583C35B730B218B542E80203F5F4BC6F1E9E577, ___position_0)); }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  get_position_0() const { return ___position_0; }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720 * get_address_of_position_0() { return &___position_0; }
	inline void set_position_0(Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  value)
	{
		___position_0 = value;
	}

	inline static int32_t get_offset_of_normal_1() { return static_cast<int32_t>(offsetof(UIVertex_t0583C35B730B218B542E80203F5F4BC6F1E9E577, ___normal_1)); }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  get_normal_1() const { return ___normal_1; }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720 * get_address_of_normal_1() { return &___normal_1; }
	inline void set_normal_1(Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  value)
	{
		___normal_1 = value;
	}

	inline static int32_t get_offset_of_tangent_2() { return static_cast<int32_t>(offsetof(UIVertex_t0583C35B730B218B542E80203F5F4BC6F1E9E577, ___tangent_2)); }
	inline Vector4_tD148D6428C3F8FF6CD998F82090113C2B490B76E  get_tangent_2() const { return ___tangent_2; }
	inline Vector4_tD148D6428C3F8FF6CD998F82090113C2B490B76E * get_address_of_tangent_2() { return &___tangent_2; }
	inline void set_tangent_2(Vector4_tD148D6428C3F8FF6CD998F82090113C2B490B76E  value)
	{
		___tangent_2 = value;
	}

	inline static int32_t get_offset_of_color_3() { return static_cast<int32_t>(offsetof(UIVertex_t0583C35B730B218B542E80203F5F4BC6F1E9E577, ___color_3)); }
	inline Color32_t23ABC4AE0E0BDFD2E22EE1FA0DA3904FFE5F6E23  get_color_3() const { return ___color_3; }
	inline Color32_t23ABC4AE0E0BDFD2E22EE1FA0DA3904FFE5F6E23 * get_address_of_color_3() { return &___color_3; }
	inline void set_color_3(Color32_t23ABC4AE0E0BDFD2E22EE1FA0DA3904FFE5F6E23  value)
	{
		___color_3 = value;
	}

	inline static int32_t get_offset_of_uv0_4() { return static_cast<int32_t>(offsetof(UIVertex_t0583C35B730B218B542E80203F5F4BC6F1E9E577, ___uv0_4)); }
	inline Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  get_uv0_4() const { return ___uv0_4; }
	inline Vector2_tA85D2DD88578276CA8A8796756458277E72D073D * get_address_of_uv0_4() { return &___uv0_4; }
	inline void set_uv0_4(Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  value)
	{
		___uv0_4 = value;
	}

	inline static int32_t get_offset_of_uv1_5() { return static_cast<int32_t>(offsetof(UIVertex_t0583C35B730B218B542E80203F5F4BC6F1E9E577, ___uv1_5)); }
	inline Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  get_uv1_5() const { return ___uv1_5; }
	inline Vector2_tA85D2DD88578276CA8A8796756458277E72D073D * get_address_of_uv1_5() { return &___uv1_5; }
	inline void set_uv1_5(Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  value)
	{
		___uv1_5 = value;
	}

	inline static int32_t get_offset_of_uv2_6() { return static_cast<int32_t>(offsetof(UIVertex_t0583C35B730B218B542E80203F5F4BC6F1E9E577, ___uv2_6)); }
	inline Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  get_uv2_6() const { return ___uv2_6; }
	inline Vector2_tA85D2DD88578276CA8A8796756458277E72D073D * get_address_of_uv2_6() { return &___uv2_6; }
	inline void set_uv2_6(Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  value)
	{
		___uv2_6 = value;
	}

	inline static int32_t get_offset_of_uv3_7() { return static_cast<int32_t>(offsetof(UIVertex_t0583C35B730B218B542E80203F5F4BC6F1E9E577, ___uv3_7)); }
	inline Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  get_uv3_7() const { return ___uv3_7; }
	inline Vector2_tA85D2DD88578276CA8A8796756458277E72D073D * get_address_of_uv3_7() { return &___uv3_7; }
	inline void set_uv3_7(Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  value)
	{
		___uv3_7 = value;
	}
};

struct UIVertex_t0583C35B730B218B542E80203F5F4BC6F1E9E577_StaticFields
{
public:
	// UnityEngine.Color32 UnityEngine.UIVertex::s_DefaultColor
	Color32_t23ABC4AE0E0BDFD2E22EE1FA0DA3904FFE5F6E23  ___s_DefaultColor_8;
	// UnityEngine.Vector4 UnityEngine.UIVertex::s_DefaultTangent
	Vector4_tD148D6428C3F8FF6CD998F82090113C2B490B76E  ___s_DefaultTangent_9;
	// UnityEngine.UIVertex UnityEngine.UIVertex::simpleVert
	UIVertex_t0583C35B730B218B542E80203F5F4BC6F1E9E577  ___simpleVert_10;

public:
	inline static int32_t get_offset_of_s_DefaultColor_8() { return static_cast<int32_t>(offsetof(UIVertex_t0583C35B730B218B542E80203F5F4BC6F1E9E577_StaticFields, ___s_DefaultColor_8)); }
	inline Color32_t23ABC4AE0E0BDFD2E22EE1FA0DA3904FFE5F6E23  get_s_DefaultColor_8() const { return ___s_DefaultColor_8; }
	inline Color32_t23ABC4AE0E0BDFD2E22EE1FA0DA3904FFE5F6E23 * get_address_of_s_DefaultColor_8() { return &___s_DefaultColor_8; }
	inline void set_s_DefaultColor_8(Color32_t23ABC4AE0E0BDFD2E22EE1FA0DA3904FFE5F6E23  value)
	{
		___s_DefaultColor_8 = value;
	}

	inline static int32_t get_offset_of_s_DefaultTangent_9() { return static_cast<int32_t>(offsetof(UIVertex_t0583C35B730B218B542E80203F5F4BC6F1E9E577_StaticFields, ___s_DefaultTangent_9)); }
	inline Vector4_tD148D6428C3F8FF6CD998F82090113C2B490B76E  get_s_DefaultTangent_9() const { return ___s_DefaultTangent_9; }
	inline Vector4_tD148D6428C3F8FF6CD998F82090113C2B490B76E * get_address_of_s_DefaultTangent_9() { return &___s_DefaultTangent_9; }
	inline void set_s_DefaultTangent_9(Vector4_tD148D6428C3F8FF6CD998F82090113C2B490B76E  value)
	{
		___s_DefaultTangent_9 = value;
	}

	inline static int32_t get_offset_of_simpleVert_10() { return static_cast<int32_t>(offsetof(UIVertex_t0583C35B730B218B542E80203F5F4BC6F1E9E577_StaticFields, ___simpleVert_10)); }
	inline UIVertex_t0583C35B730B218B542E80203F5F4BC6F1E9E577  get_simpleVert_10() const { return ___simpleVert_10; }
	inline UIVertex_t0583C35B730B218B542E80203F5F4BC6F1E9E577 * get_address_of_simpleVert_10() { return &___simpleVert_10; }
	inline void set_simpleVert_10(UIVertex_t0583C35B730B218B542E80203F5F4BC6F1E9E577  value)
	{
		___simpleVert_10 = value;
	}
};


// UnityEngine.Windows.Speech.ConfidenceLevel
struct  ConfidenceLevel_t56554EC6B602F1294B9E933704E2EC961906AA36 
{
public:
	// System.Int32 UnityEngine.Windows.Speech.ConfidenceLevel::value__
	int32_t ___value___2;

public:
	inline static int32_t get_offset_of_value___2() { return static_cast<int32_t>(offsetof(ConfidenceLevel_t56554EC6B602F1294B9E933704E2EC961906AA36, ___value___2)); }
	inline int32_t get_value___2() const { return ___value___2; }
	inline int32_t* get_address_of_value___2() { return &___value___2; }
	inline void set_value___2(int32_t value)
	{
		___value___2 = value;
	}
};


// UnityEngine.Windows.WebCam.PhotoCapture_CaptureResultType
struct  CaptureResultType_t0947F7AEB5339384E8BEAF02848926669FAF8620 
{
public:
	// System.Int32 UnityEngine.Windows.WebCam.PhotoCapture_CaptureResultType::value__
	int32_t ___value___2;

public:
	inline static int32_t get_offset_of_value___2() { return static_cast<int32_t>(offsetof(CaptureResultType_t0947F7AEB5339384E8BEAF02848926669FAF8620, ___value___2)); }
	inline int32_t get_value___2() const { return ___value___2; }
	inline int32_t* get_address_of_value___2() { return &___value___2; }
	inline void set_value___2(int32_t value)
	{
		___value___2 = value;
	}
};


// UnityEngine.Windows.WebCam.VideoCapture_CaptureResultType
struct  CaptureResultType_tC35B2BE9EB6ED0C7BC7FF3D1A4C47143F77D7EAE 
{
public:
	// System.Int32 UnityEngine.Windows.WebCam.VideoCapture_CaptureResultType::value__
	int32_t ___value___2;

public:
	inline static int32_t get_offset_of_value___2() { return static_cast<int32_t>(offsetof(CaptureResultType_tC35B2BE9EB6ED0C7BC7FF3D1A4C47143F77D7EAE, ___value___2)); }
	inline int32_t get_value___2() const { return ___value___2; }
	inline int32_t* get_address_of_value___2() { return &___value___2; }
	inline void set_value___2(int32_t value)
	{
		___value___2 = value;
	}
};


// UnityEngine.XR.AvailableTrackingData
struct  AvailableTrackingData_tF1140FC398AFB5CA7E9FBBBC8ECB242E91E86AAD 
{
public:
	// System.Int32 UnityEngine.XR.AvailableTrackingData::value__
	int32_t ___value___2;

public:
	inline static int32_t get_offset_of_value___2() { return static_cast<int32_t>(offsetof(AvailableTrackingData_tF1140FC398AFB5CA7E9FBBBC8ECB242E91E86AAD, ___value___2)); }
	inline int32_t get_value___2() const { return ___value___2; }
	inline int32_t* get_address_of_value___2() { return &___value___2; }
	inline void set_value___2(int32_t value)
	{
		___value___2 = value;
	}
};


// UnityEngine.XR.XRNode
struct  XRNode_tC8909A28AC7B1B4D71839715DDC1011895BA5F5F 
{
public:
	// System.Int32 UnityEngine.XR.XRNode::value__
	int32_t ___value___2;

public:
	inline static int32_t get_offset_of_value___2() { return static_cast<int32_t>(offsetof(XRNode_tC8909A28AC7B1B4D71839715DDC1011895BA5F5F, ___value___2)); }
	inline int32_t get_value___2() const { return ___value___2; }
	inline int32_t* get_address_of_value___2() { return &___value___2; }
	inline void set_value___2(int32_t value)
	{
		___value___2 = value;
	}
};


// Mirror.SyncList`1_Change<System.Int32Enum>
struct  Change_tA73B83D716F0F54FF7C49B3C2789779480325A58 
{
public:
	// Mirror.SyncList`1_Operation<T> Mirror.SyncList`1_Change::operation
	uint8_t ___operation_0;
	// System.Int32 Mirror.SyncList`1_Change::index
	int32_t ___index_1;
	// T Mirror.SyncList`1_Change::item
	int32_t ___item_2;

public:
	inline static int32_t get_offset_of_operation_0() { return static_cast<int32_t>(offsetof(Change_tA73B83D716F0F54FF7C49B3C2789779480325A58, ___operation_0)); }
	inline uint8_t get_operation_0() const { return ___operation_0; }
	inline uint8_t* get_address_of_operation_0() { return &___operation_0; }
	inline void set_operation_0(uint8_t value)
	{
		___operation_0 = value;
	}

	inline static int32_t get_offset_of_index_1() { return static_cast<int32_t>(offsetof(Change_tA73B83D716F0F54FF7C49B3C2789779480325A58, ___index_1)); }
	inline int32_t get_index_1() const { return ___index_1; }
	inline int32_t* get_address_of_index_1() { return &___index_1; }
	inline void set_index_1(int32_t value)
	{
		___index_1 = value;
	}

	inline static int32_t get_offset_of_item_2() { return static_cast<int32_t>(offsetof(Change_tA73B83D716F0F54FF7C49B3C2789779480325A58, ___item_2)); }
	inline int32_t get_item_2() const { return ___item_2; }
	inline int32_t* get_address_of_item_2() { return &___item_2; }
	inline void set_item_2(int32_t value)
	{
		___item_2 = value;
	}
};


// Mirror.SyncList`1_Change<System.Object>
struct  Change_t70C8E48E2782F7DA0BC9C1518C4FAD99E6DB80DA 
{
public:
	// Mirror.SyncList`1_Operation<T> Mirror.SyncList`1_Change::operation
	uint8_t ___operation_0;
	// System.Int32 Mirror.SyncList`1_Change::index
	int32_t ___index_1;
	// T Mirror.SyncList`1_Change::item
	RuntimeObject * ___item_2;

public:
	inline static int32_t get_offset_of_operation_0() { return static_cast<int32_t>(offsetof(Change_t70C8E48E2782F7DA0BC9C1518C4FAD99E6DB80DA, ___operation_0)); }
	inline uint8_t get_operation_0() const { return ___operation_0; }
	inline uint8_t* get_address_of_operation_0() { return &___operation_0; }
	inline void set_operation_0(uint8_t value)
	{
		___operation_0 = value;
	}

	inline static int32_t get_offset_of_index_1() { return static_cast<int32_t>(offsetof(Change_t70C8E48E2782F7DA0BC9C1518C4FAD99E6DB80DA, ___index_1)); }
	inline int32_t get_index_1() const { return ___index_1; }
	inline int32_t* get_address_of_index_1() { return &___index_1; }
	inline void set_index_1(int32_t value)
	{
		___index_1 = value;
	}

	inline static int32_t get_offset_of_item_2() { return static_cast<int32_t>(offsetof(Change_t70C8E48E2782F7DA0BC9C1518C4FAD99E6DB80DA, ___item_2)); }
	inline RuntimeObject * get_item_2() const { return ___item_2; }
	inline RuntimeObject ** get_address_of_item_2() { return &___item_2; }
	inline void set_item_2(RuntimeObject * value)
	{
		___item_2 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___item_2), (void*)value);
	}
};


// Mono.Unity.UnityTls_unitytls_errorstate
struct  unitytls_errorstate_t64FA817A583B1CD3CB1AFCFF9606F1F2782ABBE6 
{
public:
	// System.UInt32 Mono.Unity.UnityTls_unitytls_errorstate::magic
	uint32_t ___magic_0;
	// Mono.Unity.UnityTls_unitytls_error_code Mono.Unity.UnityTls_unitytls_errorstate::code
	uint32_t ___code_1;
	// System.UInt64 Mono.Unity.UnityTls_unitytls_errorstate::reserved
	uint64_t ___reserved_2;

public:
	inline static int32_t get_offset_of_magic_0() { return static_cast<int32_t>(offsetof(unitytls_errorstate_t64FA817A583B1CD3CB1AFCFF9606F1F2782ABBE6, ___magic_0)); }
	inline uint32_t get_magic_0() const { return ___magic_0; }
	inline uint32_t* get_address_of_magic_0() { return &___magic_0; }
	inline void set_magic_0(uint32_t value)
	{
		___magic_0 = value;
	}

	inline static int32_t get_offset_of_code_1() { return static_cast<int32_t>(offsetof(unitytls_errorstate_t64FA817A583B1CD3CB1AFCFF9606F1F2782ABBE6, ___code_1)); }
	inline uint32_t get_code_1() const { return ___code_1; }
	inline uint32_t* get_address_of_code_1() { return &___code_1; }
	inline void set_code_1(uint32_t value)
	{
		___code_1 = value;
	}

	inline static int32_t get_offset_of_reserved_2() { return static_cast<int32_t>(offsetof(unitytls_errorstate_t64FA817A583B1CD3CB1AFCFF9606F1F2782ABBE6, ___reserved_2)); }
	inline uint64_t get_reserved_2() const { return ___reserved_2; }
	inline uint64_t* get_address_of_reserved_2() { return &___reserved_2; }
	inline void set_reserved_2(uint64_t value)
	{
		___reserved_2 = value;
	}
};


// Mono.Unity.UnityTls_unitytls_tlsctx_protocolrange
struct  unitytls_tlsctx_protocolrange_t36243D72F83DAD47C95928314F58026DE8D38F47 
{
public:
	// Mono.Unity.UnityTls_unitytls_protocol Mono.Unity.UnityTls_unitytls_tlsctx_protocolrange::min
	uint32_t ___min_0;
	// Mono.Unity.UnityTls_unitytls_protocol Mono.Unity.UnityTls_unitytls_tlsctx_protocolrange::max
	uint32_t ___max_1;

public:
	inline static int32_t get_offset_of_min_0() { return static_cast<int32_t>(offsetof(unitytls_tlsctx_protocolrange_t36243D72F83DAD47C95928314F58026DE8D38F47, ___min_0)); }
	inline uint32_t get_min_0() const { return ___min_0; }
	inline uint32_t* get_address_of_min_0() { return &___min_0; }
	inline void set_min_0(uint32_t value)
	{
		___min_0 = value;
	}

	inline static int32_t get_offset_of_max_1() { return static_cast<int32_t>(offsetof(unitytls_tlsctx_protocolrange_t36243D72F83DAD47C95928314F58026DE8D38F47, ___max_1)); }
	inline uint32_t get_max_1() const { return ___max_1; }
	inline uint32_t* get_address_of_max_1() { return &___max_1; }
	inline void set_max_1(uint32_t value)
	{
		___max_1 = value;
	}
};


// System.ConsoleKeyInfo
struct  ConsoleKeyInfo_t5BE3CE05E8258CDB5404256E96AF7C22BC5DE768 
{
public:
	// System.Char System.ConsoleKeyInfo::_keyChar
	Il2CppChar ____keyChar_0;
	// System.ConsoleKey System.ConsoleKeyInfo::_key
	int32_t ____key_1;
	// System.ConsoleModifiers System.ConsoleKeyInfo::_mods
	int32_t ____mods_2;

public:
	inline static int32_t get_offset_of__keyChar_0() { return static_cast<int32_t>(offsetof(ConsoleKeyInfo_t5BE3CE05E8258CDB5404256E96AF7C22BC5DE768, ____keyChar_0)); }
	inline Il2CppChar get__keyChar_0() const { return ____keyChar_0; }
	inline Il2CppChar* get_address_of__keyChar_0() { return &____keyChar_0; }
	inline void set__keyChar_0(Il2CppChar value)
	{
		____keyChar_0 = value;
	}

	inline static int32_t get_offset_of__key_1() { return static_cast<int32_t>(offsetof(ConsoleKeyInfo_t5BE3CE05E8258CDB5404256E96AF7C22BC5DE768, ____key_1)); }
	inline int32_t get__key_1() const { return ____key_1; }
	inline int32_t* get_address_of__key_1() { return &____key_1; }
	inline void set__key_1(int32_t value)
	{
		____key_1 = value;
	}

	inline static int32_t get_offset_of__mods_2() { return static_cast<int32_t>(offsetof(ConsoleKeyInfo_t5BE3CE05E8258CDB5404256E96AF7C22BC5DE768, ____mods_2)); }
	inline int32_t get__mods_2() const { return ____mods_2; }
	inline int32_t* get_address_of__mods_2() { return &____mods_2; }
	inline void set__mods_2(int32_t value)
	{
		____mods_2 = value;
	}
};

// Native definition for P/Invoke marshalling of System.ConsoleKeyInfo
struct ConsoleKeyInfo_t5BE3CE05E8258CDB5404256E96AF7C22BC5DE768_marshaled_pinvoke
{
	uint8_t ____keyChar_0;
	int32_t ____key_1;
	int32_t ____mods_2;
};
// Native definition for COM marshalling of System.ConsoleKeyInfo
struct ConsoleKeyInfo_t5BE3CE05E8258CDB5404256E96AF7C22BC5DE768_marshaled_com
{
	uint8_t ____keyChar_0;
	int32_t ____key_1;
	int32_t ____mods_2;
};

// System.Runtime.Serialization.StreamingContext
struct  StreamingContext_t2CCDC54E0E8D078AF4A50E3A8B921B828A900034 
{
public:
	// System.Object System.Runtime.Serialization.StreamingContext::m_additionalContext
	RuntimeObject * ___m_additionalContext_0;
	// System.Runtime.Serialization.StreamingContextStates System.Runtime.Serialization.StreamingContext::m_state
	int32_t ___m_state_1;

public:
	inline static int32_t get_offset_of_m_additionalContext_0() { return static_cast<int32_t>(offsetof(StreamingContext_t2CCDC54E0E8D078AF4A50E3A8B921B828A900034, ___m_additionalContext_0)); }
	inline RuntimeObject * get_m_additionalContext_0() const { return ___m_additionalContext_0; }
	inline RuntimeObject ** get_address_of_m_additionalContext_0() { return &___m_additionalContext_0; }
	inline void set_m_additionalContext_0(RuntimeObject * value)
	{
		___m_additionalContext_0 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___m_additionalContext_0), (void*)value);
	}

	inline static int32_t get_offset_of_m_state_1() { return static_cast<int32_t>(offsetof(StreamingContext_t2CCDC54E0E8D078AF4A50E3A8B921B828A900034, ___m_state_1)); }
	inline int32_t get_m_state_1() const { return ___m_state_1; }
	inline int32_t* get_address_of_m_state_1() { return &___m_state_1; }
	inline void set_m_state_1(int32_t value)
	{
		___m_state_1 = value;
	}
};

// Native definition for P/Invoke marshalling of System.Runtime.Serialization.StreamingContext
struct StreamingContext_t2CCDC54E0E8D078AF4A50E3A8B921B828A900034_marshaled_pinvoke
{
	Il2CppIUnknown* ___m_additionalContext_0;
	int32_t ___m_state_1;
};
// Native definition for COM marshalling of System.Runtime.Serialization.StreamingContext
struct StreamingContext_t2CCDC54E0E8D078AF4A50E3A8B921B828A900034_marshaled_com
{
	Il2CppIUnknown* ___m_additionalContext_0;
	int32_t ___m_state_1;
};

// System.TypedReference
struct  TypedReference_t118BC3B643F75F52DB9C99D5E051299F886EB2A8 
{
public:
	// System.RuntimeTypeHandle System.TypedReference::type
	RuntimeTypeHandle_t7B542280A22F0EC4EAC2061C29178845847A8B2D  ___type_0;
	// System.IntPtr System.TypedReference::Value
	intptr_t ___Value_1;
	// System.IntPtr System.TypedReference::Type
	intptr_t ___Type_2;

public:
	inline static int32_t get_offset_of_type_0() { return static_cast<int32_t>(offsetof(TypedReference_t118BC3B643F75F52DB9C99D5E051299F886EB2A8, ___type_0)); }
	inline RuntimeTypeHandle_t7B542280A22F0EC4EAC2061C29178845847A8B2D  get_type_0() const { return ___type_0; }
	inline RuntimeTypeHandle_t7B542280A22F0EC4EAC2061C29178845847A8B2D * get_address_of_type_0() { return &___type_0; }
	inline void set_type_0(RuntimeTypeHandle_t7B542280A22F0EC4EAC2061C29178845847A8B2D  value)
	{
		___type_0 = value;
	}

	inline static int32_t get_offset_of_Value_1() { return static_cast<int32_t>(offsetof(TypedReference_t118BC3B643F75F52DB9C99D5E051299F886EB2A8, ___Value_1)); }
	inline intptr_t get_Value_1() const { return ___Value_1; }
	inline intptr_t* get_address_of_Value_1() { return &___Value_1; }
	inline void set_Value_1(intptr_t value)
	{
		___Value_1 = value;
	}

	inline static int32_t get_offset_of_Type_2() { return static_cast<int32_t>(offsetof(TypedReference_t118BC3B643F75F52DB9C99D5E051299F886EB2A8, ___Type_2)); }
	inline intptr_t get_Type_2() const { return ___Type_2; }
	inline intptr_t* get_address_of_Type_2() { return &___Type_2; }
	inline void set_Type_2(intptr_t value)
	{
		___Type_2 = value;
	}
};


// Telepathy.Message
struct  Message_tF83CFFEC28E30B669B3165790493DAE2F48D6B4E 
{
public:
	// System.Int32 Telepathy.Message::connectionId
	int32_t ___connectionId_0;
	// Telepathy.EventType Telepathy.Message::eventType
	int32_t ___eventType_1;
	// System.Byte[] Telepathy.Message::data
	ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* ___data_2;

public:
	inline static int32_t get_offset_of_connectionId_0() { return static_cast<int32_t>(offsetof(Message_tF83CFFEC28E30B669B3165790493DAE2F48D6B4E, ___connectionId_0)); }
	inline int32_t get_connectionId_0() const { return ___connectionId_0; }
	inline int32_t* get_address_of_connectionId_0() { return &___connectionId_0; }
	inline void set_connectionId_0(int32_t value)
	{
		___connectionId_0 = value;
	}

	inline static int32_t get_offset_of_eventType_1() { return static_cast<int32_t>(offsetof(Message_tF83CFFEC28E30B669B3165790493DAE2F48D6B4E, ___eventType_1)); }
	inline int32_t get_eventType_1() const { return ___eventType_1; }
	inline int32_t* get_address_of_eventType_1() { return &___eventType_1; }
	inline void set_eventType_1(int32_t value)
	{
		___eventType_1 = value;
	}

	inline static int32_t get_offset_of_data_2() { return static_cast<int32_t>(offsetof(Message_tF83CFFEC28E30B669B3165790493DAE2F48D6B4E, ___data_2)); }
	inline ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* get_data_2() const { return ___data_2; }
	inline ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821** get_address_of_data_2() { return &___data_2; }
	inline void set_data_2(ByteU5BU5D_tD06FDBE8142446525DF1C40351D523A228373821* value)
	{
		___data_2 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___data_2), (void*)value);
	}
};

// Native definition for P/Invoke marshalling of Telepathy.Message
struct Message_tF83CFFEC28E30B669B3165790493DAE2F48D6B4E_marshaled_pinvoke
{
	int32_t ___connectionId_0;
	int32_t ___eventType_1;
	Il2CppSafeArray/*I1*/* ___data_2;
};
// Native definition for COM marshalling of Telepathy.Message
struct Message_tF83CFFEC28E30B669B3165790493DAE2F48D6B4E_marshaled_com
{
	int32_t ___connectionId_0;
	int32_t ___eventType_1;
	Il2CppSafeArray/*I1*/* ___data_2;
};

// Unity.Collections.NativeArray`1<System.Int32>
struct  NativeArray_1_tC6374EC584BF0D6DD4AD6FA0FD00C2C82F82CCAF 
{
public:
	// System.Void* Unity.Collections.NativeArray`1::m_Buffer
	void* ___m_Buffer_0;
	// System.Int32 Unity.Collections.NativeArray`1::m_Length
	int32_t ___m_Length_1;
	// Unity.Collections.Allocator Unity.Collections.NativeArray`1::m_AllocatorLabel
	int32_t ___m_AllocatorLabel_2;

public:
	inline static int32_t get_offset_of_m_Buffer_0() { return static_cast<int32_t>(offsetof(NativeArray_1_tC6374EC584BF0D6DD4AD6FA0FD00C2C82F82CCAF, ___m_Buffer_0)); }
	inline void* get_m_Buffer_0() const { return ___m_Buffer_0; }
	inline void** get_address_of_m_Buffer_0() { return &___m_Buffer_0; }
	inline void set_m_Buffer_0(void* value)
	{
		___m_Buffer_0 = value;
	}

	inline static int32_t get_offset_of_m_Length_1() { return static_cast<int32_t>(offsetof(NativeArray_1_tC6374EC584BF0D6DD4AD6FA0FD00C2C82F82CCAF, ___m_Length_1)); }
	inline int32_t get_m_Length_1() const { return ___m_Length_1; }
	inline int32_t* get_address_of_m_Length_1() { return &___m_Length_1; }
	inline void set_m_Length_1(int32_t value)
	{
		___m_Length_1 = value;
	}

	inline static int32_t get_offset_of_m_AllocatorLabel_2() { return static_cast<int32_t>(offsetof(NativeArray_1_tC6374EC584BF0D6DD4AD6FA0FD00C2C82F82CCAF, ___m_AllocatorLabel_2)); }
	inline int32_t get_m_AllocatorLabel_2() const { return ___m_AllocatorLabel_2; }
	inline int32_t* get_address_of_m_AllocatorLabel_2() { return &___m_AllocatorLabel_2; }
	inline void set_m_AllocatorLabel_2(int32_t value)
	{
		___m_AllocatorLabel_2 = value;
	}
};

// Native definition for P/Invoke marshalling of Unity.Collections.NativeArray`1
#ifndef NativeArray_1_t350F3793D2FE9D9CD5A50725BE978ED846FE3098_marshaled_pinvoke_define
#define NativeArray_1_t350F3793D2FE9D9CD5A50725BE978ED846FE3098_marshaled_pinvoke_define
struct NativeArray_1_t350F3793D2FE9D9CD5A50725BE978ED846FE3098_marshaled_pinvoke
{
	void* ___m_Buffer_0;
	int32_t ___m_Length_1;
	int32_t ___m_AllocatorLabel_2;
};
#endif
// Native definition for COM marshalling of Unity.Collections.NativeArray`1
#ifndef NativeArray_1_t350F3793D2FE9D9CD5A50725BE978ED846FE3098_marshaled_com_define
#define NativeArray_1_t350F3793D2FE9D9CD5A50725BE978ED846FE3098_marshaled_com_define
struct NativeArray_1_t350F3793D2FE9D9CD5A50725BE978ED846FE3098_marshaled_com
{
	void* ___m_Buffer_0;
	int32_t ___m_Length_1;
	int32_t ___m_AllocatorLabel_2;
};
#endif

// Unity.Collections.NativeArray`1<UnityEngine.Experimental.GlobalIllumination.LightDataGI>
struct  NativeArray_1_t8C7BA311FA83977B4520E39644CB5AD8F1E9E0FE 
{
public:
	// System.Void* Unity.Collections.NativeArray`1::m_Buffer
	void* ___m_Buffer_0;
	// System.Int32 Unity.Collections.NativeArray`1::m_Length
	int32_t ___m_Length_1;
	// Unity.Collections.Allocator Unity.Collections.NativeArray`1::m_AllocatorLabel
	int32_t ___m_AllocatorLabel_2;

public:
	inline static int32_t get_offset_of_m_Buffer_0() { return static_cast<int32_t>(offsetof(NativeArray_1_t8C7BA311FA83977B4520E39644CB5AD8F1E9E0FE, ___m_Buffer_0)); }
	inline void* get_m_Buffer_0() const { return ___m_Buffer_0; }
	inline void** get_address_of_m_Buffer_0() { return &___m_Buffer_0; }
	inline void set_m_Buffer_0(void* value)
	{
		___m_Buffer_0 = value;
	}

	inline static int32_t get_offset_of_m_Length_1() { return static_cast<int32_t>(offsetof(NativeArray_1_t8C7BA311FA83977B4520E39644CB5AD8F1E9E0FE, ___m_Length_1)); }
	inline int32_t get_m_Length_1() const { return ___m_Length_1; }
	inline int32_t* get_address_of_m_Length_1() { return &___m_Length_1; }
	inline void set_m_Length_1(int32_t value)
	{
		___m_Length_1 = value;
	}

	inline static int32_t get_offset_of_m_AllocatorLabel_2() { return static_cast<int32_t>(offsetof(NativeArray_1_t8C7BA311FA83977B4520E39644CB5AD8F1E9E0FE, ___m_AllocatorLabel_2)); }
	inline int32_t get_m_AllocatorLabel_2() const { return ___m_AllocatorLabel_2; }
	inline int32_t* get_address_of_m_AllocatorLabel_2() { return &___m_AllocatorLabel_2; }
	inline void set_m_AllocatorLabel_2(int32_t value)
	{
		___m_AllocatorLabel_2 = value;
	}
};

// Native definition for P/Invoke marshalling of Unity.Collections.NativeArray`1
#ifndef NativeArray_1_t350F3793D2FE9D9CD5A50725BE978ED846FE3098_marshaled_pinvoke_define
#define NativeArray_1_t350F3793D2FE9D9CD5A50725BE978ED846FE3098_marshaled_pinvoke_define
struct NativeArray_1_t350F3793D2FE9D9CD5A50725BE978ED846FE3098_marshaled_pinvoke
{
	void* ___m_Buffer_0;
	int32_t ___m_Length_1;
	int32_t ___m_AllocatorLabel_2;
};
#endif
// Native definition for COM marshalling of Unity.Collections.NativeArray`1
#ifndef NativeArray_1_t350F3793D2FE9D9CD5A50725BE978ED846FE3098_marshaled_com_define
#define NativeArray_1_t350F3793D2FE9D9CD5A50725BE978ED846FE3098_marshaled_com_define
struct NativeArray_1_t350F3793D2FE9D9CD5A50725BE978ED846FE3098_marshaled_com
{
	void* ___m_Buffer_0;
	int32_t ___m_Length_1;
	int32_t ___m_AllocatorLabel_2;
};
#endif

// Unity.Collections.NativeArray`1<UnityEngine.Plane>
struct  NativeArray_1_t83B803BC075802611FE185566F7FE576D1B85F52 
{
public:
	// System.Void* Unity.Collections.NativeArray`1::m_Buffer
	void* ___m_Buffer_0;
	// System.Int32 Unity.Collections.NativeArray`1::m_Length
	int32_t ___m_Length_1;
	// Unity.Collections.Allocator Unity.Collections.NativeArray`1::m_AllocatorLabel
	int32_t ___m_AllocatorLabel_2;

public:
	inline static int32_t get_offset_of_m_Buffer_0() { return static_cast<int32_t>(offsetof(NativeArray_1_t83B803BC075802611FE185566F7FE576D1B85F52, ___m_Buffer_0)); }
	inline void* get_m_Buffer_0() const { return ___m_Buffer_0; }
	inline void** get_address_of_m_Buffer_0() { return &___m_Buffer_0; }
	inline void set_m_Buffer_0(void* value)
	{
		___m_Buffer_0 = value;
	}

	inline static int32_t get_offset_of_m_Length_1() { return static_cast<int32_t>(offsetof(NativeArray_1_t83B803BC075802611FE185566F7FE576D1B85F52, ___m_Length_1)); }
	inline int32_t get_m_Length_1() const { return ___m_Length_1; }
	inline int32_t* get_address_of_m_Length_1() { return &___m_Length_1; }
	inline void set_m_Length_1(int32_t value)
	{
		___m_Length_1 = value;
	}

	inline static int32_t get_offset_of_m_AllocatorLabel_2() { return static_cast<int32_t>(offsetof(NativeArray_1_t83B803BC075802611FE185566F7FE576D1B85F52, ___m_AllocatorLabel_2)); }
	inline int32_t get_m_AllocatorLabel_2() const { return ___m_AllocatorLabel_2; }
	inline int32_t* get_address_of_m_AllocatorLabel_2() { return &___m_AllocatorLabel_2; }
	inline void set_m_AllocatorLabel_2(int32_t value)
	{
		___m_AllocatorLabel_2 = value;
	}
};

// Native definition for P/Invoke marshalling of Unity.Collections.NativeArray`1
#ifndef NativeArray_1_t350F3793D2FE9D9CD5A50725BE978ED846FE3098_marshaled_pinvoke_define
#define NativeArray_1_t350F3793D2FE9D9CD5A50725BE978ED846FE3098_marshaled_pinvoke_define
struct NativeArray_1_t350F3793D2FE9D9CD5A50725BE978ED846FE3098_marshaled_pinvoke
{
	void* ___m_Buffer_0;
	int32_t ___m_Length_1;
	int32_t ___m_AllocatorLabel_2;
};
#endif
// Native definition for COM marshalling of Unity.Collections.NativeArray`1
#ifndef NativeArray_1_t350F3793D2FE9D9CD5A50725BE978ED846FE3098_marshaled_com_define
#define NativeArray_1_t350F3793D2FE9D9CD5A50725BE978ED846FE3098_marshaled_com_define
struct NativeArray_1_t350F3793D2FE9D9CD5A50725BE978ED846FE3098_marshaled_com
{
	void* ___m_Buffer_0;
	int32_t ___m_Length_1;
	int32_t ___m_AllocatorLabel_2;
};
#endif

// Unity.Collections.NativeArray`1<UnityEngine.Rendering.BatchVisibility>
struct  NativeArray_1_t1D9423FECCE6FE0EBBFAB0CF4124B39FF8B66520 
{
public:
	// System.Void* Unity.Collections.NativeArray`1::m_Buffer
	void* ___m_Buffer_0;
	// System.Int32 Unity.Collections.NativeArray`1::m_Length
	int32_t ___m_Length_1;
	// Unity.Collections.Allocator Unity.Collections.NativeArray`1::m_AllocatorLabel
	int32_t ___m_AllocatorLabel_2;

public:
	inline static int32_t get_offset_of_m_Buffer_0() { return static_cast<int32_t>(offsetof(NativeArray_1_t1D9423FECCE6FE0EBBFAB0CF4124B39FF8B66520, ___m_Buffer_0)); }
	inline void* get_m_Buffer_0() const { return ___m_Buffer_0; }
	inline void** get_address_of_m_Buffer_0() { return &___m_Buffer_0; }
	inline void set_m_Buffer_0(void* value)
	{
		___m_Buffer_0 = value;
	}

	inline static int32_t get_offset_of_m_Length_1() { return static_cast<int32_t>(offsetof(NativeArray_1_t1D9423FECCE6FE0EBBFAB0CF4124B39FF8B66520, ___m_Length_1)); }
	inline int32_t get_m_Length_1() const { return ___m_Length_1; }
	inline int32_t* get_address_of_m_Length_1() { return &___m_Length_1; }
	inline void set_m_Length_1(int32_t value)
	{
		___m_Length_1 = value;
	}

	inline static int32_t get_offset_of_m_AllocatorLabel_2() { return static_cast<int32_t>(offsetof(NativeArray_1_t1D9423FECCE6FE0EBBFAB0CF4124B39FF8B66520, ___m_AllocatorLabel_2)); }
	inline int32_t get_m_AllocatorLabel_2() const { return ___m_AllocatorLabel_2; }
	inline int32_t* get_address_of_m_AllocatorLabel_2() { return &___m_AllocatorLabel_2; }
	inline void set_m_AllocatorLabel_2(int32_t value)
	{
		___m_AllocatorLabel_2 = value;
	}
};

// Native definition for P/Invoke marshalling of Unity.Collections.NativeArray`1
#ifndef NativeArray_1_t350F3793D2FE9D9CD5A50725BE978ED846FE3098_marshaled_pinvoke_define
#define NativeArray_1_t350F3793D2FE9D9CD5A50725BE978ED846FE3098_marshaled_pinvoke_define
struct NativeArray_1_t350F3793D2FE9D9CD5A50725BE978ED846FE3098_marshaled_pinvoke
{
	void* ___m_Buffer_0;
	int32_t ___m_Length_1;
	int32_t ___m_AllocatorLabel_2;
};
#endif
// Native definition for COM marshalling of Unity.Collections.NativeArray`1
#ifndef NativeArray_1_t350F3793D2FE9D9CD5A50725BE978ED846FE3098_marshaled_com_define
#define NativeArray_1_t350F3793D2FE9D9CD5A50725BE978ED846FE3098_marshaled_com_define
struct NativeArray_1_t350F3793D2FE9D9CD5A50725BE978ED846FE3098_marshaled_com
{
	void* ___m_Buffer_0;
	int32_t ___m_Length_1;
	int32_t ___m_AllocatorLabel_2;
};
#endif

// UnityEngine.Experimental.XR.BoundedPlane
struct  BoundedPlane_tBFBBCCD2AB87AEBD14E6A168EFAA0680862814D9 
{
public:
	// System.UInt32 UnityEngine.Experimental.XR.BoundedPlane::m_InstanceId
	uint32_t ___m_InstanceId_0;
	// UnityEngine.Experimental.XR.TrackableId UnityEngine.Experimental.XR.BoundedPlane::<Id>k__BackingField
	TrackableId_tA539F57E82A04D410FE11E10ACC830CF7CD71F7B  ___U3CIdU3Ek__BackingField_1;
	// UnityEngine.Experimental.XR.TrackableId UnityEngine.Experimental.XR.BoundedPlane::<SubsumedById>k__BackingField
	TrackableId_tA539F57E82A04D410FE11E10ACC830CF7CD71F7B  ___U3CSubsumedByIdU3Ek__BackingField_2;
	// UnityEngine.Pose UnityEngine.Experimental.XR.BoundedPlane::<Pose>k__BackingField
	Pose_t2997DE3CB3863E4D78FCF42B46FC481818823F29  ___U3CPoseU3Ek__BackingField_3;
	// UnityEngine.Vector3 UnityEngine.Experimental.XR.BoundedPlane::<Center>k__BackingField
	Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  ___U3CCenterU3Ek__BackingField_4;
	// UnityEngine.Vector2 UnityEngine.Experimental.XR.BoundedPlane::<Size>k__BackingField
	Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  ___U3CSizeU3Ek__BackingField_5;
	// UnityEngine.Experimental.XR.PlaneAlignment UnityEngine.Experimental.XR.BoundedPlane::<Alignment>k__BackingField
	int32_t ___U3CAlignmentU3Ek__BackingField_6;

public:
	inline static int32_t get_offset_of_m_InstanceId_0() { return static_cast<int32_t>(offsetof(BoundedPlane_tBFBBCCD2AB87AEBD14E6A168EFAA0680862814D9, ___m_InstanceId_0)); }
	inline uint32_t get_m_InstanceId_0() const { return ___m_InstanceId_0; }
	inline uint32_t* get_address_of_m_InstanceId_0() { return &___m_InstanceId_0; }
	inline void set_m_InstanceId_0(uint32_t value)
	{
		___m_InstanceId_0 = value;
	}

	inline static int32_t get_offset_of_U3CIdU3Ek__BackingField_1() { return static_cast<int32_t>(offsetof(BoundedPlane_tBFBBCCD2AB87AEBD14E6A168EFAA0680862814D9, ___U3CIdU3Ek__BackingField_1)); }
	inline TrackableId_tA539F57E82A04D410FE11E10ACC830CF7CD71F7B  get_U3CIdU3Ek__BackingField_1() const { return ___U3CIdU3Ek__BackingField_1; }
	inline TrackableId_tA539F57E82A04D410FE11E10ACC830CF7CD71F7B * get_address_of_U3CIdU3Ek__BackingField_1() { return &___U3CIdU3Ek__BackingField_1; }
	inline void set_U3CIdU3Ek__BackingField_1(TrackableId_tA539F57E82A04D410FE11E10ACC830CF7CD71F7B  value)
	{
		___U3CIdU3Ek__BackingField_1 = value;
	}

	inline static int32_t get_offset_of_U3CSubsumedByIdU3Ek__BackingField_2() { return static_cast<int32_t>(offsetof(BoundedPlane_tBFBBCCD2AB87AEBD14E6A168EFAA0680862814D9, ___U3CSubsumedByIdU3Ek__BackingField_2)); }
	inline TrackableId_tA539F57E82A04D410FE11E10ACC830CF7CD71F7B  get_U3CSubsumedByIdU3Ek__BackingField_2() const { return ___U3CSubsumedByIdU3Ek__BackingField_2; }
	inline TrackableId_tA539F57E82A04D410FE11E10ACC830CF7CD71F7B * get_address_of_U3CSubsumedByIdU3Ek__BackingField_2() { return &___U3CSubsumedByIdU3Ek__BackingField_2; }
	inline void set_U3CSubsumedByIdU3Ek__BackingField_2(TrackableId_tA539F57E82A04D410FE11E10ACC830CF7CD71F7B  value)
	{
		___U3CSubsumedByIdU3Ek__BackingField_2 = value;
	}

	inline static int32_t get_offset_of_U3CPoseU3Ek__BackingField_3() { return static_cast<int32_t>(offsetof(BoundedPlane_tBFBBCCD2AB87AEBD14E6A168EFAA0680862814D9, ___U3CPoseU3Ek__BackingField_3)); }
	inline Pose_t2997DE3CB3863E4D78FCF42B46FC481818823F29  get_U3CPoseU3Ek__BackingField_3() const { return ___U3CPoseU3Ek__BackingField_3; }
	inline Pose_t2997DE3CB3863E4D78FCF42B46FC481818823F29 * get_address_of_U3CPoseU3Ek__BackingField_3() { return &___U3CPoseU3Ek__BackingField_3; }
	inline void set_U3CPoseU3Ek__BackingField_3(Pose_t2997DE3CB3863E4D78FCF42B46FC481818823F29  value)
	{
		___U3CPoseU3Ek__BackingField_3 = value;
	}

	inline static int32_t get_offset_of_U3CCenterU3Ek__BackingField_4() { return static_cast<int32_t>(offsetof(BoundedPlane_tBFBBCCD2AB87AEBD14E6A168EFAA0680862814D9, ___U3CCenterU3Ek__BackingField_4)); }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  get_U3CCenterU3Ek__BackingField_4() const { return ___U3CCenterU3Ek__BackingField_4; }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720 * get_address_of_U3CCenterU3Ek__BackingField_4() { return &___U3CCenterU3Ek__BackingField_4; }
	inline void set_U3CCenterU3Ek__BackingField_4(Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  value)
	{
		___U3CCenterU3Ek__BackingField_4 = value;
	}

	inline static int32_t get_offset_of_U3CSizeU3Ek__BackingField_5() { return static_cast<int32_t>(offsetof(BoundedPlane_tBFBBCCD2AB87AEBD14E6A168EFAA0680862814D9, ___U3CSizeU3Ek__BackingField_5)); }
	inline Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  get_U3CSizeU3Ek__BackingField_5() const { return ___U3CSizeU3Ek__BackingField_5; }
	inline Vector2_tA85D2DD88578276CA8A8796756458277E72D073D * get_address_of_U3CSizeU3Ek__BackingField_5() { return &___U3CSizeU3Ek__BackingField_5; }
	inline void set_U3CSizeU3Ek__BackingField_5(Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  value)
	{
		___U3CSizeU3Ek__BackingField_5 = value;
	}

	inline static int32_t get_offset_of_U3CAlignmentU3Ek__BackingField_6() { return static_cast<int32_t>(offsetof(BoundedPlane_tBFBBCCD2AB87AEBD14E6A168EFAA0680862814D9, ___U3CAlignmentU3Ek__BackingField_6)); }
	inline int32_t get_U3CAlignmentU3Ek__BackingField_6() const { return ___U3CAlignmentU3Ek__BackingField_6; }
	inline int32_t* get_address_of_U3CAlignmentU3Ek__BackingField_6() { return &___U3CAlignmentU3Ek__BackingField_6; }
	inline void set_U3CAlignmentU3Ek__BackingField_6(int32_t value)
	{
		___U3CAlignmentU3Ek__BackingField_6 = value;
	}
};


// UnityEngine.Experimental.XR.MeshGenerationResult
struct  MeshGenerationResult_t24F21E71F8F697D7D216BA4F3F064FB5434E6056 
{
public:
	// UnityEngine.Experimental.XR.TrackableId UnityEngine.Experimental.XR.MeshGenerationResult::<MeshId>k__BackingField
	TrackableId_tA539F57E82A04D410FE11E10ACC830CF7CD71F7B  ___U3CMeshIdU3Ek__BackingField_0;
	// UnityEngine.Mesh UnityEngine.Experimental.XR.MeshGenerationResult::<Mesh>k__BackingField
	Mesh_t6106B8D8E4C691321581AB0445552EC78B947B8C * ___U3CMeshU3Ek__BackingField_1;
	// UnityEngine.MeshCollider UnityEngine.Experimental.XR.MeshGenerationResult::<MeshCollider>k__BackingField
	MeshCollider_t60EB55ADE92499FE8D1AA206D2BD96E65B2766DE * ___U3CMeshColliderU3Ek__BackingField_2;
	// UnityEngine.Experimental.XR.MeshGenerationStatus UnityEngine.Experimental.XR.MeshGenerationResult::<Status>k__BackingField
	int32_t ___U3CStatusU3Ek__BackingField_3;
	// UnityEngine.Experimental.XR.MeshVertexAttributes UnityEngine.Experimental.XR.MeshGenerationResult::<Attributes>k__BackingField
	int32_t ___U3CAttributesU3Ek__BackingField_4;

public:
	inline static int32_t get_offset_of_U3CMeshIdU3Ek__BackingField_0() { return static_cast<int32_t>(offsetof(MeshGenerationResult_t24F21E71F8F697D7D216BA4F3F064FB5434E6056, ___U3CMeshIdU3Ek__BackingField_0)); }
	inline TrackableId_tA539F57E82A04D410FE11E10ACC830CF7CD71F7B  get_U3CMeshIdU3Ek__BackingField_0() const { return ___U3CMeshIdU3Ek__BackingField_0; }
	inline TrackableId_tA539F57E82A04D410FE11E10ACC830CF7CD71F7B * get_address_of_U3CMeshIdU3Ek__BackingField_0() { return &___U3CMeshIdU3Ek__BackingField_0; }
	inline void set_U3CMeshIdU3Ek__BackingField_0(TrackableId_tA539F57E82A04D410FE11E10ACC830CF7CD71F7B  value)
	{
		___U3CMeshIdU3Ek__BackingField_0 = value;
	}

	inline static int32_t get_offset_of_U3CMeshU3Ek__BackingField_1() { return static_cast<int32_t>(offsetof(MeshGenerationResult_t24F21E71F8F697D7D216BA4F3F064FB5434E6056, ___U3CMeshU3Ek__BackingField_1)); }
	inline Mesh_t6106B8D8E4C691321581AB0445552EC78B947B8C * get_U3CMeshU3Ek__BackingField_1() const { return ___U3CMeshU3Ek__BackingField_1; }
	inline Mesh_t6106B8D8E4C691321581AB0445552EC78B947B8C ** get_address_of_U3CMeshU3Ek__BackingField_1() { return &___U3CMeshU3Ek__BackingField_1; }
	inline void set_U3CMeshU3Ek__BackingField_1(Mesh_t6106B8D8E4C691321581AB0445552EC78B947B8C * value)
	{
		___U3CMeshU3Ek__BackingField_1 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___U3CMeshU3Ek__BackingField_1), (void*)value);
	}

	inline static int32_t get_offset_of_U3CMeshColliderU3Ek__BackingField_2() { return static_cast<int32_t>(offsetof(MeshGenerationResult_t24F21E71F8F697D7D216BA4F3F064FB5434E6056, ___U3CMeshColliderU3Ek__BackingField_2)); }
	inline MeshCollider_t60EB55ADE92499FE8D1AA206D2BD96E65B2766DE * get_U3CMeshColliderU3Ek__BackingField_2() const { return ___U3CMeshColliderU3Ek__BackingField_2; }
	inline MeshCollider_t60EB55ADE92499FE8D1AA206D2BD96E65B2766DE ** get_address_of_U3CMeshColliderU3Ek__BackingField_2() { return &___U3CMeshColliderU3Ek__BackingField_2; }
	inline void set_U3CMeshColliderU3Ek__BackingField_2(MeshCollider_t60EB55ADE92499FE8D1AA206D2BD96E65B2766DE * value)
	{
		___U3CMeshColliderU3Ek__BackingField_2 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___U3CMeshColliderU3Ek__BackingField_2), (void*)value);
	}

	inline static int32_t get_offset_of_U3CStatusU3Ek__BackingField_3() { return static_cast<int32_t>(offsetof(MeshGenerationResult_t24F21E71F8F697D7D216BA4F3F064FB5434E6056, ___U3CStatusU3Ek__BackingField_3)); }
	inline int32_t get_U3CStatusU3Ek__BackingField_3() const { return ___U3CStatusU3Ek__BackingField_3; }
	inline int32_t* get_address_of_U3CStatusU3Ek__BackingField_3() { return &___U3CStatusU3Ek__BackingField_3; }
	inline void set_U3CStatusU3Ek__BackingField_3(int32_t value)
	{
		___U3CStatusU3Ek__BackingField_3 = value;
	}

	inline static int32_t get_offset_of_U3CAttributesU3Ek__BackingField_4() { return static_cast<int32_t>(offsetof(MeshGenerationResult_t24F21E71F8F697D7D216BA4F3F064FB5434E6056, ___U3CAttributesU3Ek__BackingField_4)); }
	inline int32_t get_U3CAttributesU3Ek__BackingField_4() const { return ___U3CAttributesU3Ek__BackingField_4; }
	inline int32_t* get_address_of_U3CAttributesU3Ek__BackingField_4() { return &___U3CAttributesU3Ek__BackingField_4; }
	inline void set_U3CAttributesU3Ek__BackingField_4(int32_t value)
	{
		___U3CAttributesU3Ek__BackingField_4 = value;
	}
};

// Native definition for P/Invoke marshalling of UnityEngine.Experimental.XR.MeshGenerationResult
struct MeshGenerationResult_t24F21E71F8F697D7D216BA4F3F064FB5434E6056_marshaled_pinvoke
{
	TrackableId_tA539F57E82A04D410FE11E10ACC830CF7CD71F7B  ___U3CMeshIdU3Ek__BackingField_0;
	Mesh_t6106B8D8E4C691321581AB0445552EC78B947B8C * ___U3CMeshU3Ek__BackingField_1;
	MeshCollider_t60EB55ADE92499FE8D1AA206D2BD96E65B2766DE * ___U3CMeshColliderU3Ek__BackingField_2;
	int32_t ___U3CStatusU3Ek__BackingField_3;
	int32_t ___U3CAttributesU3Ek__BackingField_4;
};
// Native definition for COM marshalling of UnityEngine.Experimental.XR.MeshGenerationResult
struct MeshGenerationResult_t24F21E71F8F697D7D216BA4F3F064FB5434E6056_marshaled_com
{
	TrackableId_tA539F57E82A04D410FE11E10ACC830CF7CD71F7B  ___U3CMeshIdU3Ek__BackingField_0;
	Mesh_t6106B8D8E4C691321581AB0445552EC78B947B8C * ___U3CMeshU3Ek__BackingField_1;
	MeshCollider_t60EB55ADE92499FE8D1AA206D2BD96E65B2766DE * ___U3CMeshColliderU3Ek__BackingField_2;
	int32_t ___U3CStatusU3Ek__BackingField_3;
	int32_t ___U3CAttributesU3Ek__BackingField_4;
};

// UnityEngine.Experimental.XR.NativeGestureHoldEvent
struct  NativeGestureHoldEvent_t730220DBDB536EA39C176DF1AEDA918E180472DD 
{
public:
	// UnityEngine.Experimental.XR.GestureEventState UnityEngine.Experimental.XR.NativeGestureHoldEvent::<eventState>k__BackingField
	uint32_t ___U3CeventStateU3Ek__BackingField_0;
	// System.Int64 UnityEngine.Experimental.XR.NativeGestureHoldEvent::<time>k__BackingField
	int64_t ___U3CtimeU3Ek__BackingField_1;
	// System.UInt32 UnityEngine.Experimental.XR.NativeGestureHoldEvent::<internalDeviceId>k__BackingField
	uint32_t ___U3CinternalDeviceIdU3Ek__BackingField_2;
	// UnityEngine.Pose UnityEngine.Experimental.XR.NativeGestureHoldEvent::<pointerPose>k__BackingField
	Pose_t2997DE3CB3863E4D78FCF42B46FC481818823F29  ___U3CpointerPoseU3Ek__BackingField_3;
	// UnityEngine.Experimental.XR.GestureHoldValidFields UnityEngine.Experimental.XR.NativeGestureHoldEvent::<validFields>k__BackingField
	uint32_t ___U3CvalidFieldsU3Ek__BackingField_4;

public:
	inline static int32_t get_offset_of_U3CeventStateU3Ek__BackingField_0() { return static_cast<int32_t>(offsetof(NativeGestureHoldEvent_t730220DBDB536EA39C176DF1AEDA918E180472DD, ___U3CeventStateU3Ek__BackingField_0)); }
	inline uint32_t get_U3CeventStateU3Ek__BackingField_0() const { return ___U3CeventStateU3Ek__BackingField_0; }
	inline uint32_t* get_address_of_U3CeventStateU3Ek__BackingField_0() { return &___U3CeventStateU3Ek__BackingField_0; }
	inline void set_U3CeventStateU3Ek__BackingField_0(uint32_t value)
	{
		___U3CeventStateU3Ek__BackingField_0 = value;
	}

	inline static int32_t get_offset_of_U3CtimeU3Ek__BackingField_1() { return static_cast<int32_t>(offsetof(NativeGestureHoldEvent_t730220DBDB536EA39C176DF1AEDA918E180472DD, ___U3CtimeU3Ek__BackingField_1)); }
	inline int64_t get_U3CtimeU3Ek__BackingField_1() const { return ___U3CtimeU3Ek__BackingField_1; }
	inline int64_t* get_address_of_U3CtimeU3Ek__BackingField_1() { return &___U3CtimeU3Ek__BackingField_1; }
	inline void set_U3CtimeU3Ek__BackingField_1(int64_t value)
	{
		___U3CtimeU3Ek__BackingField_1 = value;
	}

	inline static int32_t get_offset_of_U3CinternalDeviceIdU3Ek__BackingField_2() { return static_cast<int32_t>(offsetof(NativeGestureHoldEvent_t730220DBDB536EA39C176DF1AEDA918E180472DD, ___U3CinternalDeviceIdU3Ek__BackingField_2)); }
	inline uint32_t get_U3CinternalDeviceIdU3Ek__BackingField_2() const { return ___U3CinternalDeviceIdU3Ek__BackingField_2; }
	inline uint32_t* get_address_of_U3CinternalDeviceIdU3Ek__BackingField_2() { return &___U3CinternalDeviceIdU3Ek__BackingField_2; }
	inline void set_U3CinternalDeviceIdU3Ek__BackingField_2(uint32_t value)
	{
		___U3CinternalDeviceIdU3Ek__BackingField_2 = value;
	}

	inline static int32_t get_offset_of_U3CpointerPoseU3Ek__BackingField_3() { return static_cast<int32_t>(offsetof(NativeGestureHoldEvent_t730220DBDB536EA39C176DF1AEDA918E180472DD, ___U3CpointerPoseU3Ek__BackingField_3)); }
	inline Pose_t2997DE3CB3863E4D78FCF42B46FC481818823F29  get_U3CpointerPoseU3Ek__BackingField_3() const { return ___U3CpointerPoseU3Ek__BackingField_3; }
	inline Pose_t2997DE3CB3863E4D78FCF42B46FC481818823F29 * get_address_of_U3CpointerPoseU3Ek__BackingField_3() { return &___U3CpointerPoseU3Ek__BackingField_3; }
	inline void set_U3CpointerPoseU3Ek__BackingField_3(Pose_t2997DE3CB3863E4D78FCF42B46FC481818823F29  value)
	{
		___U3CpointerPoseU3Ek__BackingField_3 = value;
	}

	inline static int32_t get_offset_of_U3CvalidFieldsU3Ek__BackingField_4() { return static_cast<int32_t>(offsetof(NativeGestureHoldEvent_t730220DBDB536EA39C176DF1AEDA918E180472DD, ___U3CvalidFieldsU3Ek__BackingField_4)); }
	inline uint32_t get_U3CvalidFieldsU3Ek__BackingField_4() const { return ___U3CvalidFieldsU3Ek__BackingField_4; }
	inline uint32_t* get_address_of_U3CvalidFieldsU3Ek__BackingField_4() { return &___U3CvalidFieldsU3Ek__BackingField_4; }
	inline void set_U3CvalidFieldsU3Ek__BackingField_4(uint32_t value)
	{
		___U3CvalidFieldsU3Ek__BackingField_4 = value;
	}
};


// UnityEngine.Experimental.XR.NativeGestureManipulationEvent
struct  NativeGestureManipulationEvent_t89FDDF7354D6EAB482DC00A9B0ED9205E82E4321 
{
public:
	// UnityEngine.Experimental.XR.GestureEventState UnityEngine.Experimental.XR.NativeGestureManipulationEvent::<eventState>k__BackingField
	uint32_t ___U3CeventStateU3Ek__BackingField_0;
	// System.Int64 UnityEngine.Experimental.XR.NativeGestureManipulationEvent::<time>k__BackingField
	int64_t ___U3CtimeU3Ek__BackingField_1;
	// System.UInt32 UnityEngine.Experimental.XR.NativeGestureManipulationEvent::<internalDeviceId>k__BackingField
	uint32_t ___U3CinternalDeviceIdU3Ek__BackingField_2;
	// UnityEngine.Vector3 UnityEngine.Experimental.XR.NativeGestureManipulationEvent::<translation>k__BackingField
	Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  ___U3CtranslationU3Ek__BackingField_3;
	// UnityEngine.Pose UnityEngine.Experimental.XR.NativeGestureManipulationEvent::<pointerPose>k__BackingField
	Pose_t2997DE3CB3863E4D78FCF42B46FC481818823F29  ___U3CpointerPoseU3Ek__BackingField_4;
	// UnityEngine.Experimental.XR.GestureManipulationValidFields UnityEngine.Experimental.XR.NativeGestureManipulationEvent::<validFields>k__BackingField
	uint32_t ___U3CvalidFieldsU3Ek__BackingField_5;

public:
	inline static int32_t get_offset_of_U3CeventStateU3Ek__BackingField_0() { return static_cast<int32_t>(offsetof(NativeGestureManipulationEvent_t89FDDF7354D6EAB482DC00A9B0ED9205E82E4321, ___U3CeventStateU3Ek__BackingField_0)); }
	inline uint32_t get_U3CeventStateU3Ek__BackingField_0() const { return ___U3CeventStateU3Ek__BackingField_0; }
	inline uint32_t* get_address_of_U3CeventStateU3Ek__BackingField_0() { return &___U3CeventStateU3Ek__BackingField_0; }
	inline void set_U3CeventStateU3Ek__BackingField_0(uint32_t value)
	{
		___U3CeventStateU3Ek__BackingField_0 = value;
	}

	inline static int32_t get_offset_of_U3CtimeU3Ek__BackingField_1() { return static_cast<int32_t>(offsetof(NativeGestureManipulationEvent_t89FDDF7354D6EAB482DC00A9B0ED9205E82E4321, ___U3CtimeU3Ek__BackingField_1)); }
	inline int64_t get_U3CtimeU3Ek__BackingField_1() const { return ___U3CtimeU3Ek__BackingField_1; }
	inline int64_t* get_address_of_U3CtimeU3Ek__BackingField_1() { return &___U3CtimeU3Ek__BackingField_1; }
	inline void set_U3CtimeU3Ek__BackingField_1(int64_t value)
	{
		___U3CtimeU3Ek__BackingField_1 = value;
	}

	inline static int32_t get_offset_of_U3CinternalDeviceIdU3Ek__BackingField_2() { return static_cast<int32_t>(offsetof(NativeGestureManipulationEvent_t89FDDF7354D6EAB482DC00A9B0ED9205E82E4321, ___U3CinternalDeviceIdU3Ek__BackingField_2)); }
	inline uint32_t get_U3CinternalDeviceIdU3Ek__BackingField_2() const { return ___U3CinternalDeviceIdU3Ek__BackingField_2; }
	inline uint32_t* get_address_of_U3CinternalDeviceIdU3Ek__BackingField_2() { return &___U3CinternalDeviceIdU3Ek__BackingField_2; }
	inline void set_U3CinternalDeviceIdU3Ek__BackingField_2(uint32_t value)
	{
		___U3CinternalDeviceIdU3Ek__BackingField_2 = value;
	}

	inline static int32_t get_offset_of_U3CtranslationU3Ek__BackingField_3() { return static_cast<int32_t>(offsetof(NativeGestureManipulationEvent_t89FDDF7354D6EAB482DC00A9B0ED9205E82E4321, ___U3CtranslationU3Ek__BackingField_3)); }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  get_U3CtranslationU3Ek__BackingField_3() const { return ___U3CtranslationU3Ek__BackingField_3; }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720 * get_address_of_U3CtranslationU3Ek__BackingField_3() { return &___U3CtranslationU3Ek__BackingField_3; }
	inline void set_U3CtranslationU3Ek__BackingField_3(Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  value)
	{
		___U3CtranslationU3Ek__BackingField_3 = value;
	}

	inline static int32_t get_offset_of_U3CpointerPoseU3Ek__BackingField_4() { return static_cast<int32_t>(offsetof(NativeGestureManipulationEvent_t89FDDF7354D6EAB482DC00A9B0ED9205E82E4321, ___U3CpointerPoseU3Ek__BackingField_4)); }
	inline Pose_t2997DE3CB3863E4D78FCF42B46FC481818823F29  get_U3CpointerPoseU3Ek__BackingField_4() const { return ___U3CpointerPoseU3Ek__BackingField_4; }
	inline Pose_t2997DE3CB3863E4D78FCF42B46FC481818823F29 * get_address_of_U3CpointerPoseU3Ek__BackingField_4() { return &___U3CpointerPoseU3Ek__BackingField_4; }
	inline void set_U3CpointerPoseU3Ek__BackingField_4(Pose_t2997DE3CB3863E4D78FCF42B46FC481818823F29  value)
	{
		___U3CpointerPoseU3Ek__BackingField_4 = value;
	}

	inline static int32_t get_offset_of_U3CvalidFieldsU3Ek__BackingField_5() { return static_cast<int32_t>(offsetof(NativeGestureManipulationEvent_t89FDDF7354D6EAB482DC00A9B0ED9205E82E4321, ___U3CvalidFieldsU3Ek__BackingField_5)); }
	inline uint32_t get_U3CvalidFieldsU3Ek__BackingField_5() const { return ___U3CvalidFieldsU3Ek__BackingField_5; }
	inline uint32_t* get_address_of_U3CvalidFieldsU3Ek__BackingField_5() { return &___U3CvalidFieldsU3Ek__BackingField_5; }
	inline void set_U3CvalidFieldsU3Ek__BackingField_5(uint32_t value)
	{
		___U3CvalidFieldsU3Ek__BackingField_5 = value;
	}
};


// UnityEngine.Experimental.XR.NativeGestureNavigationEvent
struct  NativeGestureNavigationEvent_t7A61BC08AC1387B9A0F756E471D7FE47B7834890 
{
public:
	// UnityEngine.Experimental.XR.GestureEventState UnityEngine.Experimental.XR.NativeGestureNavigationEvent::<eventState>k__BackingField
	uint32_t ___U3CeventStateU3Ek__BackingField_0;
	// System.Int64 UnityEngine.Experimental.XR.NativeGestureNavigationEvent::<time>k__BackingField
	int64_t ___U3CtimeU3Ek__BackingField_1;
	// System.UInt32 UnityEngine.Experimental.XR.NativeGestureNavigationEvent::<internalDeviceId>k__BackingField
	uint32_t ___U3CinternalDeviceIdU3Ek__BackingField_2;
	// UnityEngine.Experimental.XR.GestureTrackingCoordinates UnityEngine.Experimental.XR.NativeGestureNavigationEvent::<gestureTrackingCoordinates>k__BackingField
	uint32_t ___U3CgestureTrackingCoordinatesU3Ek__BackingField_3;
	// UnityEngine.Vector3 UnityEngine.Experimental.XR.NativeGestureNavigationEvent::<normalizedOffset>k__BackingField
	Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  ___U3CnormalizedOffsetU3Ek__BackingField_4;
	// UnityEngine.Pose UnityEngine.Experimental.XR.NativeGestureNavigationEvent::<pointerPose>k__BackingField
	Pose_t2997DE3CB3863E4D78FCF42B46FC481818823F29  ___U3CpointerPoseU3Ek__BackingField_5;
	// UnityEngine.Experimental.XR.GestureNavigationValidFields UnityEngine.Experimental.XR.NativeGestureNavigationEvent::<validFields>k__BackingField
	uint32_t ___U3CvalidFieldsU3Ek__BackingField_6;

public:
	inline static int32_t get_offset_of_U3CeventStateU3Ek__BackingField_0() { return static_cast<int32_t>(offsetof(NativeGestureNavigationEvent_t7A61BC08AC1387B9A0F756E471D7FE47B7834890, ___U3CeventStateU3Ek__BackingField_0)); }
	inline uint32_t get_U3CeventStateU3Ek__BackingField_0() const { return ___U3CeventStateU3Ek__BackingField_0; }
	inline uint32_t* get_address_of_U3CeventStateU3Ek__BackingField_0() { return &___U3CeventStateU3Ek__BackingField_0; }
	inline void set_U3CeventStateU3Ek__BackingField_0(uint32_t value)
	{
		___U3CeventStateU3Ek__BackingField_0 = value;
	}

	inline static int32_t get_offset_of_U3CtimeU3Ek__BackingField_1() { return static_cast<int32_t>(offsetof(NativeGestureNavigationEvent_t7A61BC08AC1387B9A0F756E471D7FE47B7834890, ___U3CtimeU3Ek__BackingField_1)); }
	inline int64_t get_U3CtimeU3Ek__BackingField_1() const { return ___U3CtimeU3Ek__BackingField_1; }
	inline int64_t* get_address_of_U3CtimeU3Ek__BackingField_1() { return &___U3CtimeU3Ek__BackingField_1; }
	inline void set_U3CtimeU3Ek__BackingField_1(int64_t value)
	{
		___U3CtimeU3Ek__BackingField_1 = value;
	}

	inline static int32_t get_offset_of_U3CinternalDeviceIdU3Ek__BackingField_2() { return static_cast<int32_t>(offsetof(NativeGestureNavigationEvent_t7A61BC08AC1387B9A0F756E471D7FE47B7834890, ___U3CinternalDeviceIdU3Ek__BackingField_2)); }
	inline uint32_t get_U3CinternalDeviceIdU3Ek__BackingField_2() const { return ___U3CinternalDeviceIdU3Ek__BackingField_2; }
	inline uint32_t* get_address_of_U3CinternalDeviceIdU3Ek__BackingField_2() { return &___U3CinternalDeviceIdU3Ek__BackingField_2; }
	inline void set_U3CinternalDeviceIdU3Ek__BackingField_2(uint32_t value)
	{
		___U3CinternalDeviceIdU3Ek__BackingField_2 = value;
	}

	inline static int32_t get_offset_of_U3CgestureTrackingCoordinatesU3Ek__BackingField_3() { return static_cast<int32_t>(offsetof(NativeGestureNavigationEvent_t7A61BC08AC1387B9A0F756E471D7FE47B7834890, ___U3CgestureTrackingCoordinatesU3Ek__BackingField_3)); }
	inline uint32_t get_U3CgestureTrackingCoordinatesU3Ek__BackingField_3() const { return ___U3CgestureTrackingCoordinatesU3Ek__BackingField_3; }
	inline uint32_t* get_address_of_U3CgestureTrackingCoordinatesU3Ek__BackingField_3() { return &___U3CgestureTrackingCoordinatesU3Ek__BackingField_3; }
	inline void set_U3CgestureTrackingCoordinatesU3Ek__BackingField_3(uint32_t value)
	{
		___U3CgestureTrackingCoordinatesU3Ek__BackingField_3 = value;
	}

	inline static int32_t get_offset_of_U3CnormalizedOffsetU3Ek__BackingField_4() { return static_cast<int32_t>(offsetof(NativeGestureNavigationEvent_t7A61BC08AC1387B9A0F756E471D7FE47B7834890, ___U3CnormalizedOffsetU3Ek__BackingField_4)); }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  get_U3CnormalizedOffsetU3Ek__BackingField_4() const { return ___U3CnormalizedOffsetU3Ek__BackingField_4; }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720 * get_address_of_U3CnormalizedOffsetU3Ek__BackingField_4() { return &___U3CnormalizedOffsetU3Ek__BackingField_4; }
	inline void set_U3CnormalizedOffsetU3Ek__BackingField_4(Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  value)
	{
		___U3CnormalizedOffsetU3Ek__BackingField_4 = value;
	}

	inline static int32_t get_offset_of_U3CpointerPoseU3Ek__BackingField_5() { return static_cast<int32_t>(offsetof(NativeGestureNavigationEvent_t7A61BC08AC1387B9A0F756E471D7FE47B7834890, ___U3CpointerPoseU3Ek__BackingField_5)); }
	inline Pose_t2997DE3CB3863E4D78FCF42B46FC481818823F29  get_U3CpointerPoseU3Ek__BackingField_5() const { return ___U3CpointerPoseU3Ek__BackingField_5; }
	inline Pose_t2997DE3CB3863E4D78FCF42B46FC481818823F29 * get_address_of_U3CpointerPoseU3Ek__BackingField_5() { return &___U3CpointerPoseU3Ek__BackingField_5; }
	inline void set_U3CpointerPoseU3Ek__BackingField_5(Pose_t2997DE3CB3863E4D78FCF42B46FC481818823F29  value)
	{
		___U3CpointerPoseU3Ek__BackingField_5 = value;
	}

	inline static int32_t get_offset_of_U3CvalidFieldsU3Ek__BackingField_6() { return static_cast<int32_t>(offsetof(NativeGestureNavigationEvent_t7A61BC08AC1387B9A0F756E471D7FE47B7834890, ___U3CvalidFieldsU3Ek__BackingField_6)); }
	inline uint32_t get_U3CvalidFieldsU3Ek__BackingField_6() const { return ___U3CvalidFieldsU3Ek__BackingField_6; }
	inline uint32_t* get_address_of_U3CvalidFieldsU3Ek__BackingField_6() { return &___U3CvalidFieldsU3Ek__BackingField_6; }
	inline void set_U3CvalidFieldsU3Ek__BackingField_6(uint32_t value)
	{
		___U3CvalidFieldsU3Ek__BackingField_6 = value;
	}
};


// UnityEngine.Experimental.XR.NativeGestureRecognitionEvent
struct  NativeGestureRecognitionEvent_t14CAF4301F20BCE91B9E879F6A1605D14DD42519 
{
public:
	// UnityEngine.Experimental.XR.GestureEventState UnityEngine.Experimental.XR.NativeGestureRecognitionEvent::<eventState>k__BackingField
	uint32_t ___U3CeventStateU3Ek__BackingField_0;
	// System.Int64 UnityEngine.Experimental.XR.NativeGestureRecognitionEvent::<time>k__BackingField
	int64_t ___U3CtimeU3Ek__BackingField_1;
	// System.UInt32 UnityEngine.Experimental.XR.NativeGestureRecognitionEvent::<internalDeviceId>k__BackingField
	uint32_t ___U3CinternalDeviceIdU3Ek__BackingField_2;
	// UnityEngine.Pose UnityEngine.Experimental.XR.NativeGestureRecognitionEvent::<pointerPose>k__BackingField
	Pose_t2997DE3CB3863E4D78FCF42B46FC481818823F29  ___U3CpointerPoseU3Ek__BackingField_3;
	// UnityEngine.Experimental.XR.GestureRecognitionValidFields UnityEngine.Experimental.XR.NativeGestureRecognitionEvent::<validFields>k__BackingField
	uint32_t ___U3CvalidFieldsU3Ek__BackingField_4;

public:
	inline static int32_t get_offset_of_U3CeventStateU3Ek__BackingField_0() { return static_cast<int32_t>(offsetof(NativeGestureRecognitionEvent_t14CAF4301F20BCE91B9E879F6A1605D14DD42519, ___U3CeventStateU3Ek__BackingField_0)); }
	inline uint32_t get_U3CeventStateU3Ek__BackingField_0() const { return ___U3CeventStateU3Ek__BackingField_0; }
	inline uint32_t* get_address_of_U3CeventStateU3Ek__BackingField_0() { return &___U3CeventStateU3Ek__BackingField_0; }
	inline void set_U3CeventStateU3Ek__BackingField_0(uint32_t value)
	{
		___U3CeventStateU3Ek__BackingField_0 = value;
	}

	inline static int32_t get_offset_of_U3CtimeU3Ek__BackingField_1() { return static_cast<int32_t>(offsetof(NativeGestureRecognitionEvent_t14CAF4301F20BCE91B9E879F6A1605D14DD42519, ___U3CtimeU3Ek__BackingField_1)); }
	inline int64_t get_U3CtimeU3Ek__BackingField_1() const { return ___U3CtimeU3Ek__BackingField_1; }
	inline int64_t* get_address_of_U3CtimeU3Ek__BackingField_1() { return &___U3CtimeU3Ek__BackingField_1; }
	inline void set_U3CtimeU3Ek__BackingField_1(int64_t value)
	{
		___U3CtimeU3Ek__BackingField_1 = value;
	}

	inline static int32_t get_offset_of_U3CinternalDeviceIdU3Ek__BackingField_2() { return static_cast<int32_t>(offsetof(NativeGestureRecognitionEvent_t14CAF4301F20BCE91B9E879F6A1605D14DD42519, ___U3CinternalDeviceIdU3Ek__BackingField_2)); }
	inline uint32_t get_U3CinternalDeviceIdU3Ek__BackingField_2() const { return ___U3CinternalDeviceIdU3Ek__BackingField_2; }
	inline uint32_t* get_address_of_U3CinternalDeviceIdU3Ek__BackingField_2() { return &___U3CinternalDeviceIdU3Ek__BackingField_2; }
	inline void set_U3CinternalDeviceIdU3Ek__BackingField_2(uint32_t value)
	{
		___U3CinternalDeviceIdU3Ek__BackingField_2 = value;
	}

	inline static int32_t get_offset_of_U3CpointerPoseU3Ek__BackingField_3() { return static_cast<int32_t>(offsetof(NativeGestureRecognitionEvent_t14CAF4301F20BCE91B9E879F6A1605D14DD42519, ___U3CpointerPoseU3Ek__BackingField_3)); }
	inline Pose_t2997DE3CB3863E4D78FCF42B46FC481818823F29  get_U3CpointerPoseU3Ek__BackingField_3() const { return ___U3CpointerPoseU3Ek__BackingField_3; }
	inline Pose_t2997DE3CB3863E4D78FCF42B46FC481818823F29 * get_address_of_U3CpointerPoseU3Ek__BackingField_3() { return &___U3CpointerPoseU3Ek__BackingField_3; }
	inline void set_U3CpointerPoseU3Ek__BackingField_3(Pose_t2997DE3CB3863E4D78FCF42B46FC481818823F29  value)
	{
		___U3CpointerPoseU3Ek__BackingField_3 = value;
	}

	inline static int32_t get_offset_of_U3CvalidFieldsU3Ek__BackingField_4() { return static_cast<int32_t>(offsetof(NativeGestureRecognitionEvent_t14CAF4301F20BCE91B9E879F6A1605D14DD42519, ___U3CvalidFieldsU3Ek__BackingField_4)); }
	inline uint32_t get_U3CvalidFieldsU3Ek__BackingField_4() const { return ___U3CvalidFieldsU3Ek__BackingField_4; }
	inline uint32_t* get_address_of_U3CvalidFieldsU3Ek__BackingField_4() { return &___U3CvalidFieldsU3Ek__BackingField_4; }
	inline void set_U3CvalidFieldsU3Ek__BackingField_4(uint32_t value)
	{
		___U3CvalidFieldsU3Ek__BackingField_4 = value;
	}
};


// UnityEngine.Experimental.XR.NativeGestureTappedEvent
struct  NativeGestureTappedEvent_tBC0278D6741BF5A2EFEA94AE6F13B94BB8F27E97 
{
public:
	// UnityEngine.Experimental.XR.GestureEventState UnityEngine.Experimental.XR.NativeGestureTappedEvent::<eventState>k__BackingField
	uint32_t ___U3CeventStateU3Ek__BackingField_0;
	// System.Int64 UnityEngine.Experimental.XR.NativeGestureTappedEvent::<time>k__BackingField
	int64_t ___U3CtimeU3Ek__BackingField_1;
	// System.UInt32 UnityEngine.Experimental.XR.NativeGestureTappedEvent::<internalDeviceId>k__BackingField
	uint32_t ___U3CinternalDeviceIdU3Ek__BackingField_2;
	// System.UInt32 UnityEngine.Experimental.XR.NativeGestureTappedEvent::<tappedCount>k__BackingField
	uint32_t ___U3CtappedCountU3Ek__BackingField_3;
	// UnityEngine.Pose UnityEngine.Experimental.XR.NativeGestureTappedEvent::<pointerPose>k__BackingField
	Pose_t2997DE3CB3863E4D78FCF42B46FC481818823F29  ___U3CpointerPoseU3Ek__BackingField_4;
	// UnityEngine.Experimental.XR.GestureTappedValidFields UnityEngine.Experimental.XR.NativeGestureTappedEvent::<validFields>k__BackingField
	uint32_t ___U3CvalidFieldsU3Ek__BackingField_5;

public:
	inline static int32_t get_offset_of_U3CeventStateU3Ek__BackingField_0() { return static_cast<int32_t>(offsetof(NativeGestureTappedEvent_tBC0278D6741BF5A2EFEA94AE6F13B94BB8F27E97, ___U3CeventStateU3Ek__BackingField_0)); }
	inline uint32_t get_U3CeventStateU3Ek__BackingField_0() const { return ___U3CeventStateU3Ek__BackingField_0; }
	inline uint32_t* get_address_of_U3CeventStateU3Ek__BackingField_0() { return &___U3CeventStateU3Ek__BackingField_0; }
	inline void set_U3CeventStateU3Ek__BackingField_0(uint32_t value)
	{
		___U3CeventStateU3Ek__BackingField_0 = value;
	}

	inline static int32_t get_offset_of_U3CtimeU3Ek__BackingField_1() { return static_cast<int32_t>(offsetof(NativeGestureTappedEvent_tBC0278D6741BF5A2EFEA94AE6F13B94BB8F27E97, ___U3CtimeU3Ek__BackingField_1)); }
	inline int64_t get_U3CtimeU3Ek__BackingField_1() const { return ___U3CtimeU3Ek__BackingField_1; }
	inline int64_t* get_address_of_U3CtimeU3Ek__BackingField_1() { return &___U3CtimeU3Ek__BackingField_1; }
	inline void set_U3CtimeU3Ek__BackingField_1(int64_t value)
	{
		___U3CtimeU3Ek__BackingField_1 = value;
	}

	inline static int32_t get_offset_of_U3CinternalDeviceIdU3Ek__BackingField_2() { return static_cast<int32_t>(offsetof(NativeGestureTappedEvent_tBC0278D6741BF5A2EFEA94AE6F13B94BB8F27E97, ___U3CinternalDeviceIdU3Ek__BackingField_2)); }
	inline uint32_t get_U3CinternalDeviceIdU3Ek__BackingField_2() const { return ___U3CinternalDeviceIdU3Ek__BackingField_2; }
	inline uint32_t* get_address_of_U3CinternalDeviceIdU3Ek__BackingField_2() { return &___U3CinternalDeviceIdU3Ek__BackingField_2; }
	inline void set_U3CinternalDeviceIdU3Ek__BackingField_2(uint32_t value)
	{
		___U3CinternalDeviceIdU3Ek__BackingField_2 = value;
	}

	inline static int32_t get_offset_of_U3CtappedCountU3Ek__BackingField_3() { return static_cast<int32_t>(offsetof(NativeGestureTappedEvent_tBC0278D6741BF5A2EFEA94AE6F13B94BB8F27E97, ___U3CtappedCountU3Ek__BackingField_3)); }
	inline uint32_t get_U3CtappedCountU3Ek__BackingField_3() const { return ___U3CtappedCountU3Ek__BackingField_3; }
	inline uint32_t* get_address_of_U3CtappedCountU3Ek__BackingField_3() { return &___U3CtappedCountU3Ek__BackingField_3; }
	inline void set_U3CtappedCountU3Ek__BackingField_3(uint32_t value)
	{
		___U3CtappedCountU3Ek__BackingField_3 = value;
	}

	inline static int32_t get_offset_of_U3CpointerPoseU3Ek__BackingField_4() { return static_cast<int32_t>(offsetof(NativeGestureTappedEvent_tBC0278D6741BF5A2EFEA94AE6F13B94BB8F27E97, ___U3CpointerPoseU3Ek__BackingField_4)); }
	inline Pose_t2997DE3CB3863E4D78FCF42B46FC481818823F29  get_U3CpointerPoseU3Ek__BackingField_4() const { return ___U3CpointerPoseU3Ek__BackingField_4; }
	inline Pose_t2997DE3CB3863E4D78FCF42B46FC481818823F29 * get_address_of_U3CpointerPoseU3Ek__BackingField_4() { return &___U3CpointerPoseU3Ek__BackingField_4; }
	inline void set_U3CpointerPoseU3Ek__BackingField_4(Pose_t2997DE3CB3863E4D78FCF42B46FC481818823F29  value)
	{
		___U3CpointerPoseU3Ek__BackingField_4 = value;
	}

	inline static int32_t get_offset_of_U3CvalidFieldsU3Ek__BackingField_5() { return static_cast<int32_t>(offsetof(NativeGestureTappedEvent_tBC0278D6741BF5A2EFEA94AE6F13B94BB8F27E97, ___U3CvalidFieldsU3Ek__BackingField_5)); }
	inline uint32_t get_U3CvalidFieldsU3Ek__BackingField_5() const { return ___U3CvalidFieldsU3Ek__BackingField_5; }
	inline uint32_t* get_address_of_U3CvalidFieldsU3Ek__BackingField_5() { return &___U3CvalidFieldsU3Ek__BackingField_5; }
	inline void set_U3CvalidFieldsU3Ek__BackingField_5(uint32_t value)
	{
		___U3CvalidFieldsU3Ek__BackingField_5 = value;
	}
};


// UnityEngine.Experimental.XR.ReferencePoint
struct  ReferencePoint_t209849A64F7DAFF039E2519A19CA3DAE45599E7E 
{
public:
	// UnityEngine.Experimental.XR.TrackableId UnityEngine.Experimental.XR.ReferencePoint::<Id>k__BackingField
	TrackableId_tA539F57E82A04D410FE11E10ACC830CF7CD71F7B  ___U3CIdU3Ek__BackingField_0;
	// UnityEngine.Experimental.XR.TrackingState UnityEngine.Experimental.XR.ReferencePoint::<TrackingState>k__BackingField
	int32_t ___U3CTrackingStateU3Ek__BackingField_1;
	// UnityEngine.Pose UnityEngine.Experimental.XR.ReferencePoint::<Pose>k__BackingField
	Pose_t2997DE3CB3863E4D78FCF42B46FC481818823F29  ___U3CPoseU3Ek__BackingField_2;

public:
	inline static int32_t get_offset_of_U3CIdU3Ek__BackingField_0() { return static_cast<int32_t>(offsetof(ReferencePoint_t209849A64F7DAFF039E2519A19CA3DAE45599E7E, ___U3CIdU3Ek__BackingField_0)); }
	inline TrackableId_tA539F57E82A04D410FE11E10ACC830CF7CD71F7B  get_U3CIdU3Ek__BackingField_0() const { return ___U3CIdU3Ek__BackingField_0; }
	inline TrackableId_tA539F57E82A04D410FE11E10ACC830CF7CD71F7B * get_address_of_U3CIdU3Ek__BackingField_0() { return &___U3CIdU3Ek__BackingField_0; }
	inline void set_U3CIdU3Ek__BackingField_0(TrackableId_tA539F57E82A04D410FE11E10ACC830CF7CD71F7B  value)
	{
		___U3CIdU3Ek__BackingField_0 = value;
	}

	inline static int32_t get_offset_of_U3CTrackingStateU3Ek__BackingField_1() { return static_cast<int32_t>(offsetof(ReferencePoint_t209849A64F7DAFF039E2519A19CA3DAE45599E7E, ___U3CTrackingStateU3Ek__BackingField_1)); }
	inline int32_t get_U3CTrackingStateU3Ek__BackingField_1() const { return ___U3CTrackingStateU3Ek__BackingField_1; }
	inline int32_t* get_address_of_U3CTrackingStateU3Ek__BackingField_1() { return &___U3CTrackingStateU3Ek__BackingField_1; }
	inline void set_U3CTrackingStateU3Ek__BackingField_1(int32_t value)
	{
		___U3CTrackingStateU3Ek__BackingField_1 = value;
	}

	inline static int32_t get_offset_of_U3CPoseU3Ek__BackingField_2() { return static_cast<int32_t>(offsetof(ReferencePoint_t209849A64F7DAFF039E2519A19CA3DAE45599E7E, ___U3CPoseU3Ek__BackingField_2)); }
	inline Pose_t2997DE3CB3863E4D78FCF42B46FC481818823F29  get_U3CPoseU3Ek__BackingField_2() const { return ___U3CPoseU3Ek__BackingField_2; }
	inline Pose_t2997DE3CB3863E4D78FCF42B46FC481818823F29 * get_address_of_U3CPoseU3Ek__BackingField_2() { return &___U3CPoseU3Ek__BackingField_2; }
	inline void set_U3CPoseU3Ek__BackingField_2(Pose_t2997DE3CB3863E4D78FCF42B46FC481818823F29  value)
	{
		___U3CPoseU3Ek__BackingField_2 = value;
	}
};


// UnityEngine.Experimental.XR.SessionTrackingStateChangedEventArgs
struct  SessionTrackingStateChangedEventArgs_tE4B00077E5AAE143593A0BB3FA2C57237525E7BA 
{
public:
	// UnityEngine.Experimental.XR.XRSessionSubsystem UnityEngine.Experimental.XR.SessionTrackingStateChangedEventArgs::m_Session
	XRSessionSubsystem_t75B8ED54B2BF4876D83B93780A7E13D6A9F32B8F * ___m_Session_0;
	// UnityEngine.Experimental.XR.TrackingState UnityEngine.Experimental.XR.SessionTrackingStateChangedEventArgs::<NewState>k__BackingField
	int32_t ___U3CNewStateU3Ek__BackingField_1;

public:
	inline static int32_t get_offset_of_m_Session_0() { return static_cast<int32_t>(offsetof(SessionTrackingStateChangedEventArgs_tE4B00077E5AAE143593A0BB3FA2C57237525E7BA, ___m_Session_0)); }
	inline XRSessionSubsystem_t75B8ED54B2BF4876D83B93780A7E13D6A9F32B8F * get_m_Session_0() const { return ___m_Session_0; }
	inline XRSessionSubsystem_t75B8ED54B2BF4876D83B93780A7E13D6A9F32B8F ** get_address_of_m_Session_0() { return &___m_Session_0; }
	inline void set_m_Session_0(XRSessionSubsystem_t75B8ED54B2BF4876D83B93780A7E13D6A9F32B8F * value)
	{
		___m_Session_0 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___m_Session_0), (void*)value);
	}

	inline static int32_t get_offset_of_U3CNewStateU3Ek__BackingField_1() { return static_cast<int32_t>(offsetof(SessionTrackingStateChangedEventArgs_tE4B00077E5AAE143593A0BB3FA2C57237525E7BA, ___U3CNewStateU3Ek__BackingField_1)); }
	inline int32_t get_U3CNewStateU3Ek__BackingField_1() const { return ___U3CNewStateU3Ek__BackingField_1; }
	inline int32_t* get_address_of_U3CNewStateU3Ek__BackingField_1() { return &___U3CNewStateU3Ek__BackingField_1; }
	inline void set_U3CNewStateU3Ek__BackingField_1(int32_t value)
	{
		___U3CNewStateU3Ek__BackingField_1 = value;
	}
};

// Native definition for P/Invoke marshalling of UnityEngine.Experimental.XR.SessionTrackingStateChangedEventArgs
struct SessionTrackingStateChangedEventArgs_tE4B00077E5AAE143593A0BB3FA2C57237525E7BA_marshaled_pinvoke
{
	XRSessionSubsystem_t75B8ED54B2BF4876D83B93780A7E13D6A9F32B8F * ___m_Session_0;
	int32_t ___U3CNewStateU3Ek__BackingField_1;
};
// Native definition for COM marshalling of UnityEngine.Experimental.XR.SessionTrackingStateChangedEventArgs
struct SessionTrackingStateChangedEventArgs_tE4B00077E5AAE143593A0BB3FA2C57237525E7BA_marshaled_com
{
	XRSessionSubsystem_t75B8ED54B2BF4876D83B93780A7E13D6A9F32B8F * ___m_Session_0;
	int32_t ___U3CNewStateU3Ek__BackingField_1;
};

// UnityEngine.Playables.Playable
struct  Playable_t4ABB910C374FCAB6B926DA4D34A85857A59950D0 
{
public:
	// UnityEngine.Playables.PlayableHandle UnityEngine.Playables.Playable::m_Handle
	PlayableHandle_t9D3B4E540D4413CED81DDD6A24C5373BEFA1D182  ___m_Handle_0;

public:
	inline static int32_t get_offset_of_m_Handle_0() { return static_cast<int32_t>(offsetof(Playable_t4ABB910C374FCAB6B926DA4D34A85857A59950D0, ___m_Handle_0)); }
	inline PlayableHandle_t9D3B4E540D4413CED81DDD6A24C5373BEFA1D182  get_m_Handle_0() const { return ___m_Handle_0; }
	inline PlayableHandle_t9D3B4E540D4413CED81DDD6A24C5373BEFA1D182 * get_address_of_m_Handle_0() { return &___m_Handle_0; }
	inline void set_m_Handle_0(PlayableHandle_t9D3B4E540D4413CED81DDD6A24C5373BEFA1D182  value)
	{
		___m_Handle_0 = value;
	}
};

struct Playable_t4ABB910C374FCAB6B926DA4D34A85857A59950D0_StaticFields
{
public:
	// UnityEngine.Playables.Playable UnityEngine.Playables.Playable::m_NullPlayable
	Playable_t4ABB910C374FCAB6B926DA4D34A85857A59950D0  ___m_NullPlayable_1;

public:
	inline static int32_t get_offset_of_m_NullPlayable_1() { return static_cast<int32_t>(offsetof(Playable_t4ABB910C374FCAB6B926DA4D34A85857A59950D0_StaticFields, ___m_NullPlayable_1)); }
	inline Playable_t4ABB910C374FCAB6B926DA4D34A85857A59950D0  get_m_NullPlayable_1() const { return ___m_NullPlayable_1; }
	inline Playable_t4ABB910C374FCAB6B926DA4D34A85857A59950D0 * get_address_of_m_NullPlayable_1() { return &___m_NullPlayable_1; }
	inline void set_m_NullPlayable_1(Playable_t4ABB910C374FCAB6B926DA4D34A85857A59950D0  value)
	{
		___m_NullPlayable_1 = value;
	}
};


// UnityEngine.Playables.PlayableOutput
struct  PlayableOutput_t5E024C3D28C983782CD4FDB2FA5AD23998D21345 
{
public:
	// UnityEngine.Playables.PlayableOutputHandle UnityEngine.Playables.PlayableOutput::m_Handle
	PlayableOutputHandle_t0D0C9D8ACC1A4061BD4EAEB61F3EE0357052F922  ___m_Handle_0;

public:
	inline static int32_t get_offset_of_m_Handle_0() { return static_cast<int32_t>(offsetof(PlayableOutput_t5E024C3D28C983782CD4FDB2FA5AD23998D21345, ___m_Handle_0)); }
	inline PlayableOutputHandle_t0D0C9D8ACC1A4061BD4EAEB61F3EE0357052F922  get_m_Handle_0() const { return ___m_Handle_0; }
	inline PlayableOutputHandle_t0D0C9D8ACC1A4061BD4EAEB61F3EE0357052F922 * get_address_of_m_Handle_0() { return &___m_Handle_0; }
	inline void set_m_Handle_0(PlayableOutputHandle_t0D0C9D8ACC1A4061BD4EAEB61F3EE0357052F922  value)
	{
		___m_Handle_0 = value;
	}
};

struct PlayableOutput_t5E024C3D28C983782CD4FDB2FA5AD23998D21345_StaticFields
{
public:
	// UnityEngine.Playables.PlayableOutput UnityEngine.Playables.PlayableOutput::m_NullPlayableOutput
	PlayableOutput_t5E024C3D28C983782CD4FDB2FA5AD23998D21345  ___m_NullPlayableOutput_1;

public:
	inline static int32_t get_offset_of_m_NullPlayableOutput_1() { return static_cast<int32_t>(offsetof(PlayableOutput_t5E024C3D28C983782CD4FDB2FA5AD23998D21345_StaticFields, ___m_NullPlayableOutput_1)); }
	inline PlayableOutput_t5E024C3D28C983782CD4FDB2FA5AD23998D21345  get_m_NullPlayableOutput_1() const { return ___m_NullPlayableOutput_1; }
	inline PlayableOutput_t5E024C3D28C983782CD4FDB2FA5AD23998D21345 * get_address_of_m_NullPlayableOutput_1() { return &___m_NullPlayableOutput_1; }
	inline void set_m_NullPlayableOutput_1(PlayableOutput_t5E024C3D28C983782CD4FDB2FA5AD23998D21345  value)
	{
		___m_NullPlayableOutput_1 = value;
	}
};


// UnityEngine.Rendering.RenderTargetIdentifier
struct  RenderTargetIdentifier_tB7480EE944FC70E0AB7D499DB17D119EB65B0F5B 
{
public:
	// UnityEngine.Rendering.BuiltinRenderTextureType UnityEngine.Rendering.RenderTargetIdentifier::m_Type
	int32_t ___m_Type_0;
	// System.Int32 UnityEngine.Rendering.RenderTargetIdentifier::m_NameID
	int32_t ___m_NameID_1;
	// System.Int32 UnityEngine.Rendering.RenderTargetIdentifier::m_InstanceID
	int32_t ___m_InstanceID_2;
	// System.IntPtr UnityEngine.Rendering.RenderTargetIdentifier::m_BufferPointer
	intptr_t ___m_BufferPointer_3;
	// System.Int32 UnityEngine.Rendering.RenderTargetIdentifier::m_MipLevel
	int32_t ___m_MipLevel_4;
	// UnityEngine.CubemapFace UnityEngine.Rendering.RenderTargetIdentifier::m_CubeFace
	int32_t ___m_CubeFace_5;
	// System.Int32 UnityEngine.Rendering.RenderTargetIdentifier::m_DepthSlice
	int32_t ___m_DepthSlice_6;

public:
	inline static int32_t get_offset_of_m_Type_0() { return static_cast<int32_t>(offsetof(RenderTargetIdentifier_tB7480EE944FC70E0AB7D499DB17D119EB65B0F5B, ___m_Type_0)); }
	inline int32_t get_m_Type_0() const { return ___m_Type_0; }
	inline int32_t* get_address_of_m_Type_0() { return &___m_Type_0; }
	inline void set_m_Type_0(int32_t value)
	{
		___m_Type_0 = value;
	}

	inline static int32_t get_offset_of_m_NameID_1() { return static_cast<int32_t>(offsetof(RenderTargetIdentifier_tB7480EE944FC70E0AB7D499DB17D119EB65B0F5B, ___m_NameID_1)); }
	inline int32_t get_m_NameID_1() const { return ___m_NameID_1; }
	inline int32_t* get_address_of_m_NameID_1() { return &___m_NameID_1; }
	inline void set_m_NameID_1(int32_t value)
	{
		___m_NameID_1 = value;
	}

	inline static int32_t get_offset_of_m_InstanceID_2() { return static_cast<int32_t>(offsetof(RenderTargetIdentifier_tB7480EE944FC70E0AB7D499DB17D119EB65B0F5B, ___m_InstanceID_2)); }
	inline int32_t get_m_InstanceID_2() const { return ___m_InstanceID_2; }
	inline int32_t* get_address_of_m_InstanceID_2() { return &___m_InstanceID_2; }
	inline void set_m_InstanceID_2(int32_t value)
	{
		___m_InstanceID_2 = value;
	}

	inline static int32_t get_offset_of_m_BufferPointer_3() { return static_cast<int32_t>(offsetof(RenderTargetIdentifier_tB7480EE944FC70E0AB7D499DB17D119EB65B0F5B, ___m_BufferPointer_3)); }
	inline intptr_t get_m_BufferPointer_3() const { return ___m_BufferPointer_3; }
	inline intptr_t* get_address_of_m_BufferPointer_3() { return &___m_BufferPointer_3; }
	inline void set_m_BufferPointer_3(intptr_t value)
	{
		___m_BufferPointer_3 = value;
	}

	inline static int32_t get_offset_of_m_MipLevel_4() { return static_cast<int32_t>(offsetof(RenderTargetIdentifier_tB7480EE944FC70E0AB7D499DB17D119EB65B0F5B, ___m_MipLevel_4)); }
	inline int32_t get_m_MipLevel_4() const { return ___m_MipLevel_4; }
	inline int32_t* get_address_of_m_MipLevel_4() { return &___m_MipLevel_4; }
	inline void set_m_MipLevel_4(int32_t value)
	{
		___m_MipLevel_4 = value;
	}

	inline static int32_t get_offset_of_m_CubeFace_5() { return static_cast<int32_t>(offsetof(RenderTargetIdentifier_tB7480EE944FC70E0AB7D499DB17D119EB65B0F5B, ___m_CubeFace_5)); }
	inline int32_t get_m_CubeFace_5() const { return ___m_CubeFace_5; }
	inline int32_t* get_address_of_m_CubeFace_5() { return &___m_CubeFace_5; }
	inline void set_m_CubeFace_5(int32_t value)
	{
		___m_CubeFace_5 = value;
	}

	inline static int32_t get_offset_of_m_DepthSlice_6() { return static_cast<int32_t>(offsetof(RenderTargetIdentifier_tB7480EE944FC70E0AB7D499DB17D119EB65B0F5B, ___m_DepthSlice_6)); }
	inline int32_t get_m_DepthSlice_6() const { return ___m_DepthSlice_6; }
	inline int32_t* get_address_of_m_DepthSlice_6() { return &___m_DepthSlice_6; }
	inline void set_m_DepthSlice_6(int32_t value)
	{
		___m_DepthSlice_6 = value;
	}
};


// UnityEngine.Touch
struct  Touch_tAACD32535FF3FE5DD91125E0B6987B93C68D2DE8 
{
public:
	// System.Int32 UnityEngine.Touch::m_FingerId
	int32_t ___m_FingerId_0;
	// UnityEngine.Vector2 UnityEngine.Touch::m_Position
	Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  ___m_Position_1;
	// UnityEngine.Vector2 UnityEngine.Touch::m_RawPosition
	Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  ___m_RawPosition_2;
	// UnityEngine.Vector2 UnityEngine.Touch::m_PositionDelta
	Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  ___m_PositionDelta_3;
	// System.Single UnityEngine.Touch::m_TimeDelta
	float ___m_TimeDelta_4;
	// System.Int32 UnityEngine.Touch::m_TapCount
	int32_t ___m_TapCount_5;
	// UnityEngine.TouchPhase UnityEngine.Touch::m_Phase
	int32_t ___m_Phase_6;
	// UnityEngine.TouchType UnityEngine.Touch::m_Type
	int32_t ___m_Type_7;
	// System.Single UnityEngine.Touch::m_Pressure
	float ___m_Pressure_8;
	// System.Single UnityEngine.Touch::m_maximumPossiblePressure
	float ___m_maximumPossiblePressure_9;
	// System.Single UnityEngine.Touch::m_Radius
	float ___m_Radius_10;
	// System.Single UnityEngine.Touch::m_RadiusVariance
	float ___m_RadiusVariance_11;
	// System.Single UnityEngine.Touch::m_AltitudeAngle
	float ___m_AltitudeAngle_12;
	// System.Single UnityEngine.Touch::m_AzimuthAngle
	float ___m_AzimuthAngle_13;

public:
	inline static int32_t get_offset_of_m_FingerId_0() { return static_cast<int32_t>(offsetof(Touch_tAACD32535FF3FE5DD91125E0B6987B93C68D2DE8, ___m_FingerId_0)); }
	inline int32_t get_m_FingerId_0() const { return ___m_FingerId_0; }
	inline int32_t* get_address_of_m_FingerId_0() { return &___m_FingerId_0; }
	inline void set_m_FingerId_0(int32_t value)
	{
		___m_FingerId_0 = value;
	}

	inline static int32_t get_offset_of_m_Position_1() { return static_cast<int32_t>(offsetof(Touch_tAACD32535FF3FE5DD91125E0B6987B93C68D2DE8, ___m_Position_1)); }
	inline Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  get_m_Position_1() const { return ___m_Position_1; }
	inline Vector2_tA85D2DD88578276CA8A8796756458277E72D073D * get_address_of_m_Position_1() { return &___m_Position_1; }
	inline void set_m_Position_1(Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  value)
	{
		___m_Position_1 = value;
	}

	inline static int32_t get_offset_of_m_RawPosition_2() { return static_cast<int32_t>(offsetof(Touch_tAACD32535FF3FE5DD91125E0B6987B93C68D2DE8, ___m_RawPosition_2)); }
	inline Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  get_m_RawPosition_2() const { return ___m_RawPosition_2; }
	inline Vector2_tA85D2DD88578276CA8A8796756458277E72D073D * get_address_of_m_RawPosition_2() { return &___m_RawPosition_2; }
	inline void set_m_RawPosition_2(Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  value)
	{
		___m_RawPosition_2 = value;
	}

	inline static int32_t get_offset_of_m_PositionDelta_3() { return static_cast<int32_t>(offsetof(Touch_tAACD32535FF3FE5DD91125E0B6987B93C68D2DE8, ___m_PositionDelta_3)); }
	inline Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  get_m_PositionDelta_3() const { return ___m_PositionDelta_3; }
	inline Vector2_tA85D2DD88578276CA8A8796756458277E72D073D * get_address_of_m_PositionDelta_3() { return &___m_PositionDelta_3; }
	inline void set_m_PositionDelta_3(Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  value)
	{
		___m_PositionDelta_3 = value;
	}

	inline static int32_t get_offset_of_m_TimeDelta_4() { return static_cast<int32_t>(offsetof(Touch_tAACD32535FF3FE5DD91125E0B6987B93C68D2DE8, ___m_TimeDelta_4)); }
	inline float get_m_TimeDelta_4() const { return ___m_TimeDelta_4; }
	inline float* get_address_of_m_TimeDelta_4() { return &___m_TimeDelta_4; }
	inline void set_m_TimeDelta_4(float value)
	{
		___m_TimeDelta_4 = value;
	}

	inline static int32_t get_offset_of_m_TapCount_5() { return static_cast<int32_t>(offsetof(Touch_tAACD32535FF3FE5DD91125E0B6987B93C68D2DE8, ___m_TapCount_5)); }
	inline int32_t get_m_TapCount_5() const { return ___m_TapCount_5; }
	inline int32_t* get_address_of_m_TapCount_5() { return &___m_TapCount_5; }
	inline void set_m_TapCount_5(int32_t value)
	{
		___m_TapCount_5 = value;
	}

	inline static int32_t get_offset_of_m_Phase_6() { return static_cast<int32_t>(offsetof(Touch_tAACD32535FF3FE5DD91125E0B6987B93C68D2DE8, ___m_Phase_6)); }
	inline int32_t get_m_Phase_6() const { return ___m_Phase_6; }
	inline int32_t* get_address_of_m_Phase_6() { return &___m_Phase_6; }
	inline void set_m_Phase_6(int32_t value)
	{
		___m_Phase_6 = value;
	}

	inline static int32_t get_offset_of_m_Type_7() { return static_cast<int32_t>(offsetof(Touch_tAACD32535FF3FE5DD91125E0B6987B93C68D2DE8, ___m_Type_7)); }
	inline int32_t get_m_Type_7() const { return ___m_Type_7; }
	inline int32_t* get_address_of_m_Type_7() { return &___m_Type_7; }
	inline void set_m_Type_7(int32_t value)
	{
		___m_Type_7 = value;
	}

	inline static int32_t get_offset_of_m_Pressure_8() { return static_cast<int32_t>(offsetof(Touch_tAACD32535FF3FE5DD91125E0B6987B93C68D2DE8, ___m_Pressure_8)); }
	inline float get_m_Pressure_8() const { return ___m_Pressure_8; }
	inline float* get_address_of_m_Pressure_8() { return &___m_Pressure_8; }
	inline void set_m_Pressure_8(float value)
	{
		___m_Pressure_8 = value;
	}

	inline static int32_t get_offset_of_m_maximumPossiblePressure_9() { return static_cast<int32_t>(offsetof(Touch_tAACD32535FF3FE5DD91125E0B6987B93C68D2DE8, ___m_maximumPossiblePressure_9)); }
	inline float get_m_maximumPossiblePressure_9() const { return ___m_maximumPossiblePressure_9; }
	inline float* get_address_of_m_maximumPossiblePressure_9() { return &___m_maximumPossiblePressure_9; }
	inline void set_m_maximumPossiblePressure_9(float value)
	{
		___m_maximumPossiblePressure_9 = value;
	}

	inline static int32_t get_offset_of_m_Radius_10() { return static_cast<int32_t>(offsetof(Touch_tAACD32535FF3FE5DD91125E0B6987B93C68D2DE8, ___m_Radius_10)); }
	inline float get_m_Radius_10() const { return ___m_Radius_10; }
	inline float* get_address_of_m_Radius_10() { return &___m_Radius_10; }
	inline void set_m_Radius_10(float value)
	{
		___m_Radius_10 = value;
	}

	inline static int32_t get_offset_of_m_RadiusVariance_11() { return static_cast<int32_t>(offsetof(Touch_tAACD32535FF3FE5DD91125E0B6987B93C68D2DE8, ___m_RadiusVariance_11)); }
	inline float get_m_RadiusVariance_11() const { return ___m_RadiusVariance_11; }
	inline float* get_address_of_m_RadiusVariance_11() { return &___m_RadiusVariance_11; }
	inline void set_m_RadiusVariance_11(float value)
	{
		___m_RadiusVariance_11 = value;
	}

	inline static int32_t get_offset_of_m_AltitudeAngle_12() { return static_cast<int32_t>(offsetof(Touch_tAACD32535FF3FE5DD91125E0B6987B93C68D2DE8, ___m_AltitudeAngle_12)); }
	inline float get_m_AltitudeAngle_12() const { return ___m_AltitudeAngle_12; }
	inline float* get_address_of_m_AltitudeAngle_12() { return &___m_AltitudeAngle_12; }
	inline void set_m_AltitudeAngle_12(float value)
	{
		___m_AltitudeAngle_12 = value;
	}

	inline static int32_t get_offset_of_m_AzimuthAngle_13() { return static_cast<int32_t>(offsetof(Touch_tAACD32535FF3FE5DD91125E0B6987B93C68D2DE8, ___m_AzimuthAngle_13)); }
	inline float get_m_AzimuthAngle_13() const { return ___m_AzimuthAngle_13; }
	inline float* get_address_of_m_AzimuthAngle_13() { return &___m_AzimuthAngle_13; }
	inline void set_m_AzimuthAngle_13(float value)
	{
		___m_AzimuthAngle_13 = value;
	}
};


// UnityEngine.UI.Navigation
struct  Navigation_t761250C05C09773B75F5E0D52DDCBBFE60288A07 
{
public:
	// UnityEngine.UI.Navigation_Mode UnityEngine.UI.Navigation::m_Mode
	int32_t ___m_Mode_0;
	// UnityEngine.UI.Selectable UnityEngine.UI.Navigation::m_SelectOnUp
	Selectable_tAA9065030FE0468018DEC880302F95FEA9C0133A * ___m_SelectOnUp_1;
	// UnityEngine.UI.Selectable UnityEngine.UI.Navigation::m_SelectOnDown
	Selectable_tAA9065030FE0468018DEC880302F95FEA9C0133A * ___m_SelectOnDown_2;
	// UnityEngine.UI.Selectable UnityEngine.UI.Navigation::m_SelectOnLeft
	Selectable_tAA9065030FE0468018DEC880302F95FEA9C0133A * ___m_SelectOnLeft_3;
	// UnityEngine.UI.Selectable UnityEngine.UI.Navigation::m_SelectOnRight
	Selectable_tAA9065030FE0468018DEC880302F95FEA9C0133A * ___m_SelectOnRight_4;

public:
	inline static int32_t get_offset_of_m_Mode_0() { return static_cast<int32_t>(offsetof(Navigation_t761250C05C09773B75F5E0D52DDCBBFE60288A07, ___m_Mode_0)); }
	inline int32_t get_m_Mode_0() const { return ___m_Mode_0; }
	inline int32_t* get_address_of_m_Mode_0() { return &___m_Mode_0; }
	inline void set_m_Mode_0(int32_t value)
	{
		___m_Mode_0 = value;
	}

	inline static int32_t get_offset_of_m_SelectOnUp_1() { return static_cast<int32_t>(offsetof(Navigation_t761250C05C09773B75F5E0D52DDCBBFE60288A07, ___m_SelectOnUp_1)); }
	inline Selectable_tAA9065030FE0468018DEC880302F95FEA9C0133A * get_m_SelectOnUp_1() const { return ___m_SelectOnUp_1; }
	inline Selectable_tAA9065030FE0468018DEC880302F95FEA9C0133A ** get_address_of_m_SelectOnUp_1() { return &___m_SelectOnUp_1; }
	inline void set_m_SelectOnUp_1(Selectable_tAA9065030FE0468018DEC880302F95FEA9C0133A * value)
	{
		___m_SelectOnUp_1 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___m_SelectOnUp_1), (void*)value);
	}

	inline static int32_t get_offset_of_m_SelectOnDown_2() { return static_cast<int32_t>(offsetof(Navigation_t761250C05C09773B75F5E0D52DDCBBFE60288A07, ___m_SelectOnDown_2)); }
	inline Selectable_tAA9065030FE0468018DEC880302F95FEA9C0133A * get_m_SelectOnDown_2() const { return ___m_SelectOnDown_2; }
	inline Selectable_tAA9065030FE0468018DEC880302F95FEA9C0133A ** get_address_of_m_SelectOnDown_2() { return &___m_SelectOnDown_2; }
	inline void set_m_SelectOnDown_2(Selectable_tAA9065030FE0468018DEC880302F95FEA9C0133A * value)
	{
		___m_SelectOnDown_2 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___m_SelectOnDown_2), (void*)value);
	}

	inline static int32_t get_offset_of_m_SelectOnLeft_3() { return static_cast<int32_t>(offsetof(Navigation_t761250C05C09773B75F5E0D52DDCBBFE60288A07, ___m_SelectOnLeft_3)); }
	inline Selectable_tAA9065030FE0468018DEC880302F95FEA9C0133A * get_m_SelectOnLeft_3() const { return ___m_SelectOnLeft_3; }
	inline Selectable_tAA9065030FE0468018DEC880302F95FEA9C0133A ** get_address_of_m_SelectOnLeft_3() { return &___m_SelectOnLeft_3; }
	inline void set_m_SelectOnLeft_3(Selectable_tAA9065030FE0468018DEC880302F95FEA9C0133A * value)
	{
		___m_SelectOnLeft_3 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___m_SelectOnLeft_3), (void*)value);
	}

	inline static int32_t get_offset_of_m_SelectOnRight_4() { return static_cast<int32_t>(offsetof(Navigation_t761250C05C09773B75F5E0D52DDCBBFE60288A07, ___m_SelectOnRight_4)); }
	inline Selectable_tAA9065030FE0468018DEC880302F95FEA9C0133A * get_m_SelectOnRight_4() const { return ___m_SelectOnRight_4; }
	inline Selectable_tAA9065030FE0468018DEC880302F95FEA9C0133A ** get_address_of_m_SelectOnRight_4() { return &___m_SelectOnRight_4; }
	inline void set_m_SelectOnRight_4(Selectable_tAA9065030FE0468018DEC880302F95FEA9C0133A * value)
	{
		___m_SelectOnRight_4 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___m_SelectOnRight_4), (void*)value);
	}
};

// Native definition for P/Invoke marshalling of UnityEngine.UI.Navigation
struct Navigation_t761250C05C09773B75F5E0D52DDCBBFE60288A07_marshaled_pinvoke
{
	int32_t ___m_Mode_0;
	Selectable_tAA9065030FE0468018DEC880302F95FEA9C0133A * ___m_SelectOnUp_1;
	Selectable_tAA9065030FE0468018DEC880302F95FEA9C0133A * ___m_SelectOnDown_2;
	Selectable_tAA9065030FE0468018DEC880302F95FEA9C0133A * ___m_SelectOnLeft_3;
	Selectable_tAA9065030FE0468018DEC880302F95FEA9C0133A * ___m_SelectOnRight_4;
};
// Native definition for COM marshalling of UnityEngine.UI.Navigation
struct Navigation_t761250C05C09773B75F5E0D52DDCBBFE60288A07_marshaled_com
{
	int32_t ___m_Mode_0;
	Selectable_tAA9065030FE0468018DEC880302F95FEA9C0133A * ___m_SelectOnUp_1;
	Selectable_tAA9065030FE0468018DEC880302F95FEA9C0133A * ___m_SelectOnDown_2;
	Selectable_tAA9065030FE0468018DEC880302F95FEA9C0133A * ___m_SelectOnLeft_3;
	Selectable_tAA9065030FE0468018DEC880302F95FEA9C0133A * ___m_SelectOnRight_4;
};

// UnityEngine.Windows.Speech.PhraseRecognizedEventArgs
struct  PhraseRecognizedEventArgs_t5045E5956BF185A7C661A2B56466E9C6101BAFAD 
{
public:
	// UnityEngine.Windows.Speech.ConfidenceLevel UnityEngine.Windows.Speech.PhraseRecognizedEventArgs::confidence
	int32_t ___confidence_0;
	// UnityEngine.Windows.Speech.SemanticMeaning[] UnityEngine.Windows.Speech.PhraseRecognizedEventArgs::semanticMeanings
	SemanticMeaningU5BU5D_t3FC0A968EA1C540EEA6B6F92368A430CA596D23D* ___semanticMeanings_1;
	// System.String UnityEngine.Windows.Speech.PhraseRecognizedEventArgs::text
	String_t* ___text_2;
	// System.DateTime UnityEngine.Windows.Speech.PhraseRecognizedEventArgs::phraseStartTime
	DateTime_t349B7449FBAAFF4192636E2B7A07694DA9236132  ___phraseStartTime_3;
	// System.TimeSpan UnityEngine.Windows.Speech.PhraseRecognizedEventArgs::phraseDuration
	TimeSpan_tA8069278ACE8A74D6DF7D514A9CD4432433F64C4  ___phraseDuration_4;

public:
	inline static int32_t get_offset_of_confidence_0() { return static_cast<int32_t>(offsetof(PhraseRecognizedEventArgs_t5045E5956BF185A7C661A2B56466E9C6101BAFAD, ___confidence_0)); }
	inline int32_t get_confidence_0() const { return ___confidence_0; }
	inline int32_t* get_address_of_confidence_0() { return &___confidence_0; }
	inline void set_confidence_0(int32_t value)
	{
		___confidence_0 = value;
	}

	inline static int32_t get_offset_of_semanticMeanings_1() { return static_cast<int32_t>(offsetof(PhraseRecognizedEventArgs_t5045E5956BF185A7C661A2B56466E9C6101BAFAD, ___semanticMeanings_1)); }
	inline SemanticMeaningU5BU5D_t3FC0A968EA1C540EEA6B6F92368A430CA596D23D* get_semanticMeanings_1() const { return ___semanticMeanings_1; }
	inline SemanticMeaningU5BU5D_t3FC0A968EA1C540EEA6B6F92368A430CA596D23D** get_address_of_semanticMeanings_1() { return &___semanticMeanings_1; }
	inline void set_semanticMeanings_1(SemanticMeaningU5BU5D_t3FC0A968EA1C540EEA6B6F92368A430CA596D23D* value)
	{
		___semanticMeanings_1 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___semanticMeanings_1), (void*)value);
	}

	inline static int32_t get_offset_of_text_2() { return static_cast<int32_t>(offsetof(PhraseRecognizedEventArgs_t5045E5956BF185A7C661A2B56466E9C6101BAFAD, ___text_2)); }
	inline String_t* get_text_2() const { return ___text_2; }
	inline String_t** get_address_of_text_2() { return &___text_2; }
	inline void set_text_2(String_t* value)
	{
		___text_2 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___text_2), (void*)value);
	}

	inline static int32_t get_offset_of_phraseStartTime_3() { return static_cast<int32_t>(offsetof(PhraseRecognizedEventArgs_t5045E5956BF185A7C661A2B56466E9C6101BAFAD, ___phraseStartTime_3)); }
	inline DateTime_t349B7449FBAAFF4192636E2B7A07694DA9236132  get_phraseStartTime_3() const { return ___phraseStartTime_3; }
	inline DateTime_t349B7449FBAAFF4192636E2B7A07694DA9236132 * get_address_of_phraseStartTime_3() { return &___phraseStartTime_3; }
	inline void set_phraseStartTime_3(DateTime_t349B7449FBAAFF4192636E2B7A07694DA9236132  value)
	{
		___phraseStartTime_3 = value;
	}

	inline static int32_t get_offset_of_phraseDuration_4() { return static_cast<int32_t>(offsetof(PhraseRecognizedEventArgs_t5045E5956BF185A7C661A2B56466E9C6101BAFAD, ___phraseDuration_4)); }
	inline TimeSpan_tA8069278ACE8A74D6DF7D514A9CD4432433F64C4  get_phraseDuration_4() const { return ___phraseDuration_4; }
	inline TimeSpan_tA8069278ACE8A74D6DF7D514A9CD4432433F64C4 * get_address_of_phraseDuration_4() { return &___phraseDuration_4; }
	inline void set_phraseDuration_4(TimeSpan_tA8069278ACE8A74D6DF7D514A9CD4432433F64C4  value)
	{
		___phraseDuration_4 = value;
	}
};

// Native definition for P/Invoke marshalling of UnityEngine.Windows.Speech.PhraseRecognizedEventArgs
struct PhraseRecognizedEventArgs_t5045E5956BF185A7C661A2B56466E9C6101BAFAD_marshaled_pinvoke
{
	int32_t ___confidence_0;
	SemanticMeaning_tF87995FD36CA45112E60A5F76AA211FA13351F0C_marshaled_pinvoke* ___semanticMeanings_1;
	char* ___text_2;
	DateTime_t349B7449FBAAFF4192636E2B7A07694DA9236132  ___phraseStartTime_3;
	TimeSpan_tA8069278ACE8A74D6DF7D514A9CD4432433F64C4  ___phraseDuration_4;
};
// Native definition for COM marshalling of UnityEngine.Windows.Speech.PhraseRecognizedEventArgs
struct PhraseRecognizedEventArgs_t5045E5956BF185A7C661A2B56466E9C6101BAFAD_marshaled_com
{
	int32_t ___confidence_0;
	SemanticMeaning_tF87995FD36CA45112E60A5F76AA211FA13351F0C_marshaled_com* ___semanticMeanings_1;
	Il2CppChar* ___text_2;
	DateTime_t349B7449FBAAFF4192636E2B7A07694DA9236132  ___phraseStartTime_3;
	TimeSpan_tA8069278ACE8A74D6DF7D514A9CD4432433F64C4  ___phraseDuration_4;
};

// UnityEngine.Windows.WebCam.PhotoCapture_PhotoCaptureResult
struct  PhotoCaptureResult_tCA8987284B3260E035A45A98C6B46485952A3900 
{
public:
	// UnityEngine.Windows.WebCam.PhotoCapture_CaptureResultType UnityEngine.Windows.WebCam.PhotoCapture_PhotoCaptureResult::resultType
	int32_t ___resultType_0;
	// System.Int64 UnityEngine.Windows.WebCam.PhotoCapture_PhotoCaptureResult::hResult
	int64_t ___hResult_1;

public:
	inline static int32_t get_offset_of_resultType_0() { return static_cast<int32_t>(offsetof(PhotoCaptureResult_tCA8987284B3260E035A45A98C6B46485952A3900, ___resultType_0)); }
	inline int32_t get_resultType_0() const { return ___resultType_0; }
	inline int32_t* get_address_of_resultType_0() { return &___resultType_0; }
	inline void set_resultType_0(int32_t value)
	{
		___resultType_0 = value;
	}

	inline static int32_t get_offset_of_hResult_1() { return static_cast<int32_t>(offsetof(PhotoCaptureResult_tCA8987284B3260E035A45A98C6B46485952A3900, ___hResult_1)); }
	inline int64_t get_hResult_1() const { return ___hResult_1; }
	inline int64_t* get_address_of_hResult_1() { return &___hResult_1; }
	inline void set_hResult_1(int64_t value)
	{
		___hResult_1 = value;
	}
};


// UnityEngine.Windows.WebCam.VideoCapture_VideoCaptureResult
struct  VideoCaptureResult_t68444D73B1DD8952CF970D983DFF25517C7C516A 
{
public:
	// UnityEngine.Windows.WebCam.VideoCapture_CaptureResultType UnityEngine.Windows.WebCam.VideoCapture_VideoCaptureResult::resultType
	int32_t ___resultType_0;
	// System.Int64 UnityEngine.Windows.WebCam.VideoCapture_VideoCaptureResult::hResult
	int64_t ___hResult_1;

public:
	inline static int32_t get_offset_of_resultType_0() { return static_cast<int32_t>(offsetof(VideoCaptureResult_t68444D73B1DD8952CF970D983DFF25517C7C516A, ___resultType_0)); }
	inline int32_t get_resultType_0() const { return ___resultType_0; }
	inline int32_t* get_address_of_resultType_0() { return &___resultType_0; }
	inline void set_resultType_0(int32_t value)
	{
		___resultType_0 = value;
	}

	inline static int32_t get_offset_of_hResult_1() { return static_cast<int32_t>(offsetof(VideoCaptureResult_t68444D73B1DD8952CF970D983DFF25517C7C516A, ___hResult_1)); }
	inline int64_t get_hResult_1() const { return ___hResult_1; }
	inline int64_t* get_address_of_hResult_1() { return &___hResult_1; }
	inline void set_hResult_1(int64_t value)
	{
		___hResult_1 = value;
	}
};


// UnityEngine.XR.XRNodeState
struct  XRNodeState_t927C248D649ED31F587DFE078E3FF180D98F7C0A 
{
public:
	// UnityEngine.XR.XRNode UnityEngine.XR.XRNodeState::m_Type
	int32_t ___m_Type_0;
	// UnityEngine.XR.AvailableTrackingData UnityEngine.XR.XRNodeState::m_AvailableFields
	int32_t ___m_AvailableFields_1;
	// UnityEngine.Vector3 UnityEngine.XR.XRNodeState::m_Position
	Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  ___m_Position_2;
	// UnityEngine.Quaternion UnityEngine.XR.XRNodeState::m_Rotation
	Quaternion_t319F3319A7D43FFA5D819AD6C0A98851F0095357  ___m_Rotation_3;
	// UnityEngine.Vector3 UnityEngine.XR.XRNodeState::m_Velocity
	Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  ___m_Velocity_4;
	// UnityEngine.Vector3 UnityEngine.XR.XRNodeState::m_AngularVelocity
	Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  ___m_AngularVelocity_5;
	// UnityEngine.Vector3 UnityEngine.XR.XRNodeState::m_Acceleration
	Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  ___m_Acceleration_6;
	// UnityEngine.Vector3 UnityEngine.XR.XRNodeState::m_AngularAcceleration
	Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  ___m_AngularAcceleration_7;
	// System.Int32 UnityEngine.XR.XRNodeState::m_Tracked
	int32_t ___m_Tracked_8;
	// System.UInt64 UnityEngine.XR.XRNodeState::m_UniqueID
	uint64_t ___m_UniqueID_9;

public:
	inline static int32_t get_offset_of_m_Type_0() { return static_cast<int32_t>(offsetof(XRNodeState_t927C248D649ED31F587DFE078E3FF180D98F7C0A, ___m_Type_0)); }
	inline int32_t get_m_Type_0() const { return ___m_Type_0; }
	inline int32_t* get_address_of_m_Type_0() { return &___m_Type_0; }
	inline void set_m_Type_0(int32_t value)
	{
		___m_Type_0 = value;
	}

	inline static int32_t get_offset_of_m_AvailableFields_1() { return static_cast<int32_t>(offsetof(XRNodeState_t927C248D649ED31F587DFE078E3FF180D98F7C0A, ___m_AvailableFields_1)); }
	inline int32_t get_m_AvailableFields_1() const { return ___m_AvailableFields_1; }
	inline int32_t* get_address_of_m_AvailableFields_1() { return &___m_AvailableFields_1; }
	inline void set_m_AvailableFields_1(int32_t value)
	{
		___m_AvailableFields_1 = value;
	}

	inline static int32_t get_offset_of_m_Position_2() { return static_cast<int32_t>(offsetof(XRNodeState_t927C248D649ED31F587DFE078E3FF180D98F7C0A, ___m_Position_2)); }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  get_m_Position_2() const { return ___m_Position_2; }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720 * get_address_of_m_Position_2() { return &___m_Position_2; }
	inline void set_m_Position_2(Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  value)
	{
		___m_Position_2 = value;
	}

	inline static int32_t get_offset_of_m_Rotation_3() { return static_cast<int32_t>(offsetof(XRNodeState_t927C248D649ED31F587DFE078E3FF180D98F7C0A, ___m_Rotation_3)); }
	inline Quaternion_t319F3319A7D43FFA5D819AD6C0A98851F0095357  get_m_Rotation_3() const { return ___m_Rotation_3; }
	inline Quaternion_t319F3319A7D43FFA5D819AD6C0A98851F0095357 * get_address_of_m_Rotation_3() { return &___m_Rotation_3; }
	inline void set_m_Rotation_3(Quaternion_t319F3319A7D43FFA5D819AD6C0A98851F0095357  value)
	{
		___m_Rotation_3 = value;
	}

	inline static int32_t get_offset_of_m_Velocity_4() { return static_cast<int32_t>(offsetof(XRNodeState_t927C248D649ED31F587DFE078E3FF180D98F7C0A, ___m_Velocity_4)); }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  get_m_Velocity_4() const { return ___m_Velocity_4; }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720 * get_address_of_m_Velocity_4() { return &___m_Velocity_4; }
	inline void set_m_Velocity_4(Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  value)
	{
		___m_Velocity_4 = value;
	}

	inline static int32_t get_offset_of_m_AngularVelocity_5() { return static_cast<int32_t>(offsetof(XRNodeState_t927C248D649ED31F587DFE078E3FF180D98F7C0A, ___m_AngularVelocity_5)); }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  get_m_AngularVelocity_5() const { return ___m_AngularVelocity_5; }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720 * get_address_of_m_AngularVelocity_5() { return &___m_AngularVelocity_5; }
	inline void set_m_AngularVelocity_5(Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  value)
	{
		___m_AngularVelocity_5 = value;
	}

	inline static int32_t get_offset_of_m_Acceleration_6() { return static_cast<int32_t>(offsetof(XRNodeState_t927C248D649ED31F587DFE078E3FF180D98F7C0A, ___m_Acceleration_6)); }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  get_m_Acceleration_6() const { return ___m_Acceleration_6; }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720 * get_address_of_m_Acceleration_6() { return &___m_Acceleration_6; }
	inline void set_m_Acceleration_6(Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  value)
	{
		___m_Acceleration_6 = value;
	}

	inline static int32_t get_offset_of_m_AngularAcceleration_7() { return static_cast<int32_t>(offsetof(XRNodeState_t927C248D649ED31F587DFE078E3FF180D98F7C0A, ___m_AngularAcceleration_7)); }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  get_m_AngularAcceleration_7() const { return ___m_AngularAcceleration_7; }
	inline Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720 * get_address_of_m_AngularAcceleration_7() { return &___m_AngularAcceleration_7; }
	inline void set_m_AngularAcceleration_7(Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  value)
	{
		___m_AngularAcceleration_7 = value;
	}

	inline static int32_t get_offset_of_m_Tracked_8() { return static_cast<int32_t>(offsetof(XRNodeState_t927C248D649ED31F587DFE078E3FF180D98F7C0A, ___m_Tracked_8)); }
	inline int32_t get_m_Tracked_8() const { return ___m_Tracked_8; }
	inline int32_t* get_address_of_m_Tracked_8() { return &___m_Tracked_8; }
	inline void set_m_Tracked_8(int32_t value)
	{
		___m_Tracked_8 = value;
	}

	inline static int32_t get_offset_of_m_UniqueID_9() { return static_cast<int32_t>(offsetof(XRNodeState_t927C248D649ED31F587DFE078E3FF180D98F7C0A, ___m_UniqueID_9)); }
	inline uint64_t get_m_UniqueID_9() const { return ___m_UniqueID_9; }
	inline uint64_t* get_address_of_m_UniqueID_9() { return &___m_UniqueID_9; }
	inline void set_m_UniqueID_9(uint64_t value)
	{
		___m_UniqueID_9 = value;
	}
};


// UnityEngine.Experimental.XR.GestureHoldEvent
struct  GestureHoldEvent_tE6ACA548C0C4D8F8C3B197066BD5E7B46A6B576A 
{
public:
	// UnityEngine.Experimental.XR.NativeGestureHoldEvent UnityEngine.Experimental.XR.GestureHoldEvent::<nativeEvent>k__BackingField
	NativeGestureHoldEvent_t730220DBDB536EA39C176DF1AEDA918E180472DD  ___U3CnativeEventU3Ek__BackingField_0;
	// UnityEngine.Experimental.XR.XRGestureSubsystem UnityEngine.Experimental.XR.GestureHoldEvent::<gestureSubsystem>k__BackingField
	XRGestureSubsystem_t3B96ED0176B1F522E14CFC2C44F688F8FC3FAA7D * ___U3CgestureSubsystemU3Ek__BackingField_1;

public:
	inline static int32_t get_offset_of_U3CnativeEventU3Ek__BackingField_0() { return static_cast<int32_t>(offsetof(GestureHoldEvent_tE6ACA548C0C4D8F8C3B197066BD5E7B46A6B576A, ___U3CnativeEventU3Ek__BackingField_0)); }
	inline NativeGestureHoldEvent_t730220DBDB536EA39C176DF1AEDA918E180472DD  get_U3CnativeEventU3Ek__BackingField_0() const { return ___U3CnativeEventU3Ek__BackingField_0; }
	inline NativeGestureHoldEvent_t730220DBDB536EA39C176DF1AEDA918E180472DD * get_address_of_U3CnativeEventU3Ek__BackingField_0() { return &___U3CnativeEventU3Ek__BackingField_0; }
	inline void set_U3CnativeEventU3Ek__BackingField_0(NativeGestureHoldEvent_t730220DBDB536EA39C176DF1AEDA918E180472DD  value)
	{
		___U3CnativeEventU3Ek__BackingField_0 = value;
	}

	inline static int32_t get_offset_of_U3CgestureSubsystemU3Ek__BackingField_1() { return static_cast<int32_t>(offsetof(GestureHoldEvent_tE6ACA548C0C4D8F8C3B197066BD5E7B46A6B576A, ___U3CgestureSubsystemU3Ek__BackingField_1)); }
	inline XRGestureSubsystem_t3B96ED0176B1F522E14CFC2C44F688F8FC3FAA7D * get_U3CgestureSubsystemU3Ek__BackingField_1() const { return ___U3CgestureSubsystemU3Ek__BackingField_1; }
	inline XRGestureSubsystem_t3B96ED0176B1F522E14CFC2C44F688F8FC3FAA7D ** get_address_of_U3CgestureSubsystemU3Ek__BackingField_1() { return &___U3CgestureSubsystemU3Ek__BackingField_1; }
	inline void set_U3CgestureSubsystemU3Ek__BackingField_1(XRGestureSubsystem_t3B96ED0176B1F522E14CFC2C44F688F8FC3FAA7D * value)
	{
		___U3CgestureSubsystemU3Ek__BackingField_1 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___U3CgestureSubsystemU3Ek__BackingField_1), (void*)value);
	}
};

// Native definition for P/Invoke marshalling of UnityEngine.Experimental.XR.GestureHoldEvent
struct GestureHoldEvent_tE6ACA548C0C4D8F8C3B197066BD5E7B46A6B576A_marshaled_pinvoke
{
	NativeGestureHoldEvent_t730220DBDB536EA39C176DF1AEDA918E180472DD  ___U3CnativeEventU3Ek__BackingField_0;
	XRGestureSubsystem_t3B96ED0176B1F522E14CFC2C44F688F8FC3FAA7D * ___U3CgestureSubsystemU3Ek__BackingField_1;
};
// Native definition for COM marshalling of UnityEngine.Experimental.XR.GestureHoldEvent
struct GestureHoldEvent_tE6ACA548C0C4D8F8C3B197066BD5E7B46A6B576A_marshaled_com
{
	NativeGestureHoldEvent_t730220DBDB536EA39C176DF1AEDA918E180472DD  ___U3CnativeEventU3Ek__BackingField_0;
	XRGestureSubsystem_t3B96ED0176B1F522E14CFC2C44F688F8FC3FAA7D * ___U3CgestureSubsystemU3Ek__BackingField_1;
};

// UnityEngine.Experimental.XR.GestureManipulationEvent
struct  GestureManipulationEvent_t80F0163CC6437F2E1BC5B773E9599AF499D08E4F 
{
public:
	// UnityEngine.Experimental.XR.NativeGestureManipulationEvent UnityEngine.Experimental.XR.GestureManipulationEvent::<nativeEvent>k__BackingField
	NativeGestureManipulationEvent_t89FDDF7354D6EAB482DC00A9B0ED9205E82E4321  ___U3CnativeEventU3Ek__BackingField_0;
	// UnityEngine.Experimental.XR.XRGestureSubsystem UnityEngine.Experimental.XR.GestureManipulationEvent::<gestureSubsystem>k__BackingField
	XRGestureSubsystem_t3B96ED0176B1F522E14CFC2C44F688F8FC3FAA7D * ___U3CgestureSubsystemU3Ek__BackingField_1;

public:
	inline static int32_t get_offset_of_U3CnativeEventU3Ek__BackingField_0() { return static_cast<int32_t>(offsetof(GestureManipulationEvent_t80F0163CC6437F2E1BC5B773E9599AF499D08E4F, ___U3CnativeEventU3Ek__BackingField_0)); }
	inline NativeGestureManipulationEvent_t89FDDF7354D6EAB482DC00A9B0ED9205E82E4321  get_U3CnativeEventU3Ek__BackingField_0() const { return ___U3CnativeEventU3Ek__BackingField_0; }
	inline NativeGestureManipulationEvent_t89FDDF7354D6EAB482DC00A9B0ED9205E82E4321 * get_address_of_U3CnativeEventU3Ek__BackingField_0() { return &___U3CnativeEventU3Ek__BackingField_0; }
	inline void set_U3CnativeEventU3Ek__BackingField_0(NativeGestureManipulationEvent_t89FDDF7354D6EAB482DC00A9B0ED9205E82E4321  value)
	{
		___U3CnativeEventU3Ek__BackingField_0 = value;
	}

	inline static int32_t get_offset_of_U3CgestureSubsystemU3Ek__BackingField_1() { return static_cast<int32_t>(offsetof(GestureManipulationEvent_t80F0163CC6437F2E1BC5B773E9599AF499D08E4F, ___U3CgestureSubsystemU3Ek__BackingField_1)); }
	inline XRGestureSubsystem_t3B96ED0176B1F522E14CFC2C44F688F8FC3FAA7D * get_U3CgestureSubsystemU3Ek__BackingField_1() const { return ___U3CgestureSubsystemU3Ek__BackingField_1; }
	inline XRGestureSubsystem_t3B96ED0176B1F522E14CFC2C44F688F8FC3FAA7D ** get_address_of_U3CgestureSubsystemU3Ek__BackingField_1() { return &___U3CgestureSubsystemU3Ek__BackingField_1; }
	inline void set_U3CgestureSubsystemU3Ek__BackingField_1(XRGestureSubsystem_t3B96ED0176B1F522E14CFC2C44F688F8FC3FAA7D * value)
	{
		___U3CgestureSubsystemU3Ek__BackingField_1 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___U3CgestureSubsystemU3Ek__BackingField_1), (void*)value);
	}
};

// Native definition for P/Invoke marshalling of UnityEngine.Experimental.XR.GestureManipulationEvent
struct GestureManipulationEvent_t80F0163CC6437F2E1BC5B773E9599AF499D08E4F_marshaled_pinvoke
{
	NativeGestureManipulationEvent_t89FDDF7354D6EAB482DC00A9B0ED9205E82E4321  ___U3CnativeEventU3Ek__BackingField_0;
	XRGestureSubsystem_t3B96ED0176B1F522E14CFC2C44F688F8FC3FAA7D * ___U3CgestureSubsystemU3Ek__BackingField_1;
};
// Native definition for COM marshalling of UnityEngine.Experimental.XR.GestureManipulationEvent
struct GestureManipulationEvent_t80F0163CC6437F2E1BC5B773E9599AF499D08E4F_marshaled_com
{
	NativeGestureManipulationEvent_t89FDDF7354D6EAB482DC00A9B0ED9205E82E4321  ___U3CnativeEventU3Ek__BackingField_0;
	XRGestureSubsystem_t3B96ED0176B1F522E14CFC2C44F688F8FC3FAA7D * ___U3CgestureSubsystemU3Ek__BackingField_1;
};

// UnityEngine.Experimental.XR.GestureNavigationEvent
struct  GestureNavigationEvent_t3718B4FDF2C71A07DFAB23CE2E27D1D022417866 
{
public:
	// UnityEngine.Experimental.XR.NativeGestureNavigationEvent UnityEngine.Experimental.XR.GestureNavigationEvent::<nativeEvent>k__BackingField
	NativeGestureNavigationEvent_t7A61BC08AC1387B9A0F756E471D7FE47B7834890  ___U3CnativeEventU3Ek__BackingField_0;
	// UnityEngine.Experimental.XR.XRGestureSubsystem UnityEngine.Experimental.XR.GestureNavigationEvent::<gestureSubsystem>k__BackingField
	XRGestureSubsystem_t3B96ED0176B1F522E14CFC2C44F688F8FC3FAA7D * ___U3CgestureSubsystemU3Ek__BackingField_1;

public:
	inline static int32_t get_offset_of_U3CnativeEventU3Ek__BackingField_0() { return static_cast<int32_t>(offsetof(GestureNavigationEvent_t3718B4FDF2C71A07DFAB23CE2E27D1D022417866, ___U3CnativeEventU3Ek__BackingField_0)); }
	inline NativeGestureNavigationEvent_t7A61BC08AC1387B9A0F756E471D7FE47B7834890  get_U3CnativeEventU3Ek__BackingField_0() const { return ___U3CnativeEventU3Ek__BackingField_0; }
	inline NativeGestureNavigationEvent_t7A61BC08AC1387B9A0F756E471D7FE47B7834890 * get_address_of_U3CnativeEventU3Ek__BackingField_0() { return &___U3CnativeEventU3Ek__BackingField_0; }
	inline void set_U3CnativeEventU3Ek__BackingField_0(NativeGestureNavigationEvent_t7A61BC08AC1387B9A0F756E471D7FE47B7834890  value)
	{
		___U3CnativeEventU3Ek__BackingField_0 = value;
	}

	inline static int32_t get_offset_of_U3CgestureSubsystemU3Ek__BackingField_1() { return static_cast<int32_t>(offsetof(GestureNavigationEvent_t3718B4FDF2C71A07DFAB23CE2E27D1D022417866, ___U3CgestureSubsystemU3Ek__BackingField_1)); }
	inline XRGestureSubsystem_t3B96ED0176B1F522E14CFC2C44F688F8FC3FAA7D * get_U3CgestureSubsystemU3Ek__BackingField_1() const { return ___U3CgestureSubsystemU3Ek__BackingField_1; }
	inline XRGestureSubsystem_t3B96ED0176B1F522E14CFC2C44F688F8FC3FAA7D ** get_address_of_U3CgestureSubsystemU3Ek__BackingField_1() { return &___U3CgestureSubsystemU3Ek__BackingField_1; }
	inline void set_U3CgestureSubsystemU3Ek__BackingField_1(XRGestureSubsystem_t3B96ED0176B1F522E14CFC2C44F688F8FC3FAA7D * value)
	{
		___U3CgestureSubsystemU3Ek__BackingField_1 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___U3CgestureSubsystemU3Ek__BackingField_1), (void*)value);
	}
};

// Native definition for P/Invoke marshalling of UnityEngine.Experimental.XR.GestureNavigationEvent
struct GestureNavigationEvent_t3718B4FDF2C71A07DFAB23CE2E27D1D022417866_marshaled_pinvoke
{
	NativeGestureNavigationEvent_t7A61BC08AC1387B9A0F756E471D7FE47B7834890  ___U3CnativeEventU3Ek__BackingField_0;
	XRGestureSubsystem_t3B96ED0176B1F522E14CFC2C44F688F8FC3FAA7D * ___U3CgestureSubsystemU3Ek__BackingField_1;
};
// Native definition for COM marshalling of UnityEngine.Experimental.XR.GestureNavigationEvent
struct GestureNavigationEvent_t3718B4FDF2C71A07DFAB23CE2E27D1D022417866_marshaled_com
{
	NativeGestureNavigationEvent_t7A61BC08AC1387B9A0F756E471D7FE47B7834890  ___U3CnativeEventU3Ek__BackingField_0;
	XRGestureSubsystem_t3B96ED0176B1F522E14CFC2C44F688F8FC3FAA7D * ___U3CgestureSubsystemU3Ek__BackingField_1;
};

// UnityEngine.Experimental.XR.GestureRecognitionEvent
struct  GestureRecognitionEvent_tBB5EFD6FF7697E5F8FDFB64BA9A51003282AEE68 
{
public:
	// UnityEngine.Experimental.XR.NativeGestureRecognitionEvent UnityEngine.Experimental.XR.GestureRecognitionEvent::<nativeEvent>k__BackingField
	NativeGestureRecognitionEvent_t14CAF4301F20BCE91B9E879F6A1605D14DD42519  ___U3CnativeEventU3Ek__BackingField_0;
	// UnityEngine.Experimental.XR.XRGestureSubsystem UnityEngine.Experimental.XR.GestureRecognitionEvent::<gestureSubsystem>k__BackingField
	XRGestureSubsystem_t3B96ED0176B1F522E14CFC2C44F688F8FC3FAA7D * ___U3CgestureSubsystemU3Ek__BackingField_1;

public:
	inline static int32_t get_offset_of_U3CnativeEventU3Ek__BackingField_0() { return static_cast<int32_t>(offsetof(GestureRecognitionEvent_tBB5EFD6FF7697E5F8FDFB64BA9A51003282AEE68, ___U3CnativeEventU3Ek__BackingField_0)); }
	inline NativeGestureRecognitionEvent_t14CAF4301F20BCE91B9E879F6A1605D14DD42519  get_U3CnativeEventU3Ek__BackingField_0() const { return ___U3CnativeEventU3Ek__BackingField_0; }
	inline NativeGestureRecognitionEvent_t14CAF4301F20BCE91B9E879F6A1605D14DD42519 * get_address_of_U3CnativeEventU3Ek__BackingField_0() { return &___U3CnativeEventU3Ek__BackingField_0; }
	inline void set_U3CnativeEventU3Ek__BackingField_0(NativeGestureRecognitionEvent_t14CAF4301F20BCE91B9E879F6A1605D14DD42519  value)
	{
		___U3CnativeEventU3Ek__BackingField_0 = value;
	}

	inline static int32_t get_offset_of_U3CgestureSubsystemU3Ek__BackingField_1() { return static_cast<int32_t>(offsetof(GestureRecognitionEvent_tBB5EFD6FF7697E5F8FDFB64BA9A51003282AEE68, ___U3CgestureSubsystemU3Ek__BackingField_1)); }
	inline XRGestureSubsystem_t3B96ED0176B1F522E14CFC2C44F688F8FC3FAA7D * get_U3CgestureSubsystemU3Ek__BackingField_1() const { return ___U3CgestureSubsystemU3Ek__BackingField_1; }
	inline XRGestureSubsystem_t3B96ED0176B1F522E14CFC2C44F688F8FC3FAA7D ** get_address_of_U3CgestureSubsystemU3Ek__BackingField_1() { return &___U3CgestureSubsystemU3Ek__BackingField_1; }
	inline void set_U3CgestureSubsystemU3Ek__BackingField_1(XRGestureSubsystem_t3B96ED0176B1F522E14CFC2C44F688F8FC3FAA7D * value)
	{
		___U3CgestureSubsystemU3Ek__BackingField_1 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___U3CgestureSubsystemU3Ek__BackingField_1), (void*)value);
	}
};

// Native definition for P/Invoke marshalling of UnityEngine.Experimental.XR.GestureRecognitionEvent
struct GestureRecognitionEvent_tBB5EFD6FF7697E5F8FDFB64BA9A51003282AEE68_marshaled_pinvoke
{
	NativeGestureRecognitionEvent_t14CAF4301F20BCE91B9E879F6A1605D14DD42519  ___U3CnativeEventU3Ek__BackingField_0;
	XRGestureSubsystem_t3B96ED0176B1F522E14CFC2C44F688F8FC3FAA7D * ___U3CgestureSubsystemU3Ek__BackingField_1;
};
// Native definition for COM marshalling of UnityEngine.Experimental.XR.GestureRecognitionEvent
struct GestureRecognitionEvent_tBB5EFD6FF7697E5F8FDFB64BA9A51003282AEE68_marshaled_com
{
	NativeGestureRecognitionEvent_t14CAF4301F20BCE91B9E879F6A1605D14DD42519  ___U3CnativeEventU3Ek__BackingField_0;
	XRGestureSubsystem_t3B96ED0176B1F522E14CFC2C44F688F8FC3FAA7D * ___U3CgestureSubsystemU3Ek__BackingField_1;
};

// UnityEngine.Experimental.XR.GestureTappedEvent
struct  GestureTappedEvent_t793E61A597E40572998004754809BBF49D738535 
{
public:
	// UnityEngine.Experimental.XR.NativeGestureTappedEvent UnityEngine.Experimental.XR.GestureTappedEvent::<nativeEvent>k__BackingField
	NativeGestureTappedEvent_tBC0278D6741BF5A2EFEA94AE6F13B94BB8F27E97  ___U3CnativeEventU3Ek__BackingField_0;
	// UnityEngine.Experimental.XR.XRGestureSubsystem UnityEngine.Experimental.XR.GestureTappedEvent::<gestureSubsystem>k__BackingField
	XRGestureSubsystem_t3B96ED0176B1F522E14CFC2C44F688F8FC3FAA7D * ___U3CgestureSubsystemU3Ek__BackingField_1;

public:
	inline static int32_t get_offset_of_U3CnativeEventU3Ek__BackingField_0() { return static_cast<int32_t>(offsetof(GestureTappedEvent_t793E61A597E40572998004754809BBF49D738535, ___U3CnativeEventU3Ek__BackingField_0)); }
	inline NativeGestureTappedEvent_tBC0278D6741BF5A2EFEA94AE6F13B94BB8F27E97  get_U3CnativeEventU3Ek__BackingField_0() const { return ___U3CnativeEventU3Ek__BackingField_0; }
	inline NativeGestureTappedEvent_tBC0278D6741BF5A2EFEA94AE6F13B94BB8F27E97 * get_address_of_U3CnativeEventU3Ek__BackingField_0() { return &___U3CnativeEventU3Ek__BackingField_0; }
	inline void set_U3CnativeEventU3Ek__BackingField_0(NativeGestureTappedEvent_tBC0278D6741BF5A2EFEA94AE6F13B94BB8F27E97  value)
	{
		___U3CnativeEventU3Ek__BackingField_0 = value;
	}

	inline static int32_t get_offset_of_U3CgestureSubsystemU3Ek__BackingField_1() { return static_cast<int32_t>(offsetof(GestureTappedEvent_t793E61A597E40572998004754809BBF49D738535, ___U3CgestureSubsystemU3Ek__BackingField_1)); }
	inline XRGestureSubsystem_t3B96ED0176B1F522E14CFC2C44F688F8FC3FAA7D * get_U3CgestureSubsystemU3Ek__BackingField_1() const { return ___U3CgestureSubsystemU3Ek__BackingField_1; }
	inline XRGestureSubsystem_t3B96ED0176B1F522E14CFC2C44F688F8FC3FAA7D ** get_address_of_U3CgestureSubsystemU3Ek__BackingField_1() { return &___U3CgestureSubsystemU3Ek__BackingField_1; }
	inline void set_U3CgestureSubsystemU3Ek__BackingField_1(XRGestureSubsystem_t3B96ED0176B1F522E14CFC2C44F688F8FC3FAA7D * value)
	{
		___U3CgestureSubsystemU3Ek__BackingField_1 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___U3CgestureSubsystemU3Ek__BackingField_1), (void*)value);
	}
};

// Native definition for P/Invoke marshalling of UnityEngine.Experimental.XR.GestureTappedEvent
struct GestureTappedEvent_t793E61A597E40572998004754809BBF49D738535_marshaled_pinvoke
{
	NativeGestureTappedEvent_tBC0278D6741BF5A2EFEA94AE6F13B94BB8F27E97  ___U3CnativeEventU3Ek__BackingField_0;
	XRGestureSubsystem_t3B96ED0176B1F522E14CFC2C44F688F8FC3FAA7D * ___U3CgestureSubsystemU3Ek__BackingField_1;
};
// Native definition for COM marshalling of UnityEngine.Experimental.XR.GestureTappedEvent
struct GestureTappedEvent_t793E61A597E40572998004754809BBF49D738535_marshaled_com
{
	NativeGestureTappedEvent_tBC0278D6741BF5A2EFEA94AE6F13B94BB8F27E97  ___U3CnativeEventU3Ek__BackingField_0;
	XRGestureSubsystem_t3B96ED0176B1F522E14CFC2C44F688F8FC3FAA7D * ___U3CgestureSubsystemU3Ek__BackingField_1;
};

// UnityEngine.Experimental.XR.PlaneAddedEventArgs
struct  PlaneAddedEventArgs_t06BF8697BA4D8CD3A8C9AB8DF51F8D01D2910002 
{
public:
	// UnityEngine.Experimental.XR.XRPlaneSubsystem UnityEngine.Experimental.XR.PlaneAddedEventArgs::<PlaneSubsystem>k__BackingField
	XRPlaneSubsystem_t3ABC3FDCBC5AC6F1CAA30E08A89EFD7F9D49D72B * ___U3CPlaneSubsystemU3Ek__BackingField_0;
	// UnityEngine.Experimental.XR.BoundedPlane UnityEngine.Experimental.XR.PlaneAddedEventArgs::<Plane>k__BackingField
	BoundedPlane_tBFBBCCD2AB87AEBD14E6A168EFAA0680862814D9  ___U3CPlaneU3Ek__BackingField_1;

public:
	inline static int32_t get_offset_of_U3CPlaneSubsystemU3Ek__BackingField_0() { return static_cast<int32_t>(offsetof(PlaneAddedEventArgs_t06BF8697BA4D8CD3A8C9AB8DF51F8D01D2910002, ___U3CPlaneSubsystemU3Ek__BackingField_0)); }
	inline XRPlaneSubsystem_t3ABC3FDCBC5AC6F1CAA30E08A89EFD7F9D49D72B * get_U3CPlaneSubsystemU3Ek__BackingField_0() const { return ___U3CPlaneSubsystemU3Ek__BackingField_0; }
	inline XRPlaneSubsystem_t3ABC3FDCBC5AC6F1CAA30E08A89EFD7F9D49D72B ** get_address_of_U3CPlaneSubsystemU3Ek__BackingField_0() { return &___U3CPlaneSubsystemU3Ek__BackingField_0; }
	inline void set_U3CPlaneSubsystemU3Ek__BackingField_0(XRPlaneSubsystem_t3ABC3FDCBC5AC6F1CAA30E08A89EFD7F9D49D72B * value)
	{
		___U3CPlaneSubsystemU3Ek__BackingField_0 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___U3CPlaneSubsystemU3Ek__BackingField_0), (void*)value);
	}

	inline static int32_t get_offset_of_U3CPlaneU3Ek__BackingField_1() { return static_cast<int32_t>(offsetof(PlaneAddedEventArgs_t06BF8697BA4D8CD3A8C9AB8DF51F8D01D2910002, ___U3CPlaneU3Ek__BackingField_1)); }
	inline BoundedPlane_tBFBBCCD2AB87AEBD14E6A168EFAA0680862814D9  get_U3CPlaneU3Ek__BackingField_1() const { return ___U3CPlaneU3Ek__BackingField_1; }
	inline BoundedPlane_tBFBBCCD2AB87AEBD14E6A168EFAA0680862814D9 * get_address_of_U3CPlaneU3Ek__BackingField_1() { return &___U3CPlaneU3Ek__BackingField_1; }
	inline void set_U3CPlaneU3Ek__BackingField_1(BoundedPlane_tBFBBCCD2AB87AEBD14E6A168EFAA0680862814D9  value)
	{
		___U3CPlaneU3Ek__BackingField_1 = value;
	}
};

// Native definition for P/Invoke marshalling of UnityEngine.Experimental.XR.PlaneAddedEventArgs
struct PlaneAddedEventArgs_t06BF8697BA4D8CD3A8C9AB8DF51F8D01D2910002_marshaled_pinvoke
{
	XRPlaneSubsystem_t3ABC3FDCBC5AC6F1CAA30E08A89EFD7F9D49D72B * ___U3CPlaneSubsystemU3Ek__BackingField_0;
	BoundedPlane_tBFBBCCD2AB87AEBD14E6A168EFAA0680862814D9  ___U3CPlaneU3Ek__BackingField_1;
};
// Native definition for COM marshalling of UnityEngine.Experimental.XR.PlaneAddedEventArgs
struct PlaneAddedEventArgs_t06BF8697BA4D8CD3A8C9AB8DF51F8D01D2910002_marshaled_com
{
	XRPlaneSubsystem_t3ABC3FDCBC5AC6F1CAA30E08A89EFD7F9D49D72B * ___U3CPlaneSubsystemU3Ek__BackingField_0;
	BoundedPlane_tBFBBCCD2AB87AEBD14E6A168EFAA0680862814D9  ___U3CPlaneU3Ek__BackingField_1;
};

// UnityEngine.Experimental.XR.PlaneRemovedEventArgs
struct  PlaneRemovedEventArgs_t21E9C5879A8317E5F72263ED2235116F609E4C6A 
{
public:
	// UnityEngine.Experimental.XR.XRPlaneSubsystem UnityEngine.Experimental.XR.PlaneRemovedEventArgs::<PlaneSubsystem>k__BackingField
	XRPlaneSubsystem_t3ABC3FDCBC5AC6F1CAA30E08A89EFD7F9D49D72B * ___U3CPlaneSubsystemU3Ek__BackingField_0;
	// UnityEngine.Experimental.XR.BoundedPlane UnityEngine.Experimental.XR.PlaneRemovedEventArgs::<Plane>k__BackingField
	BoundedPlane_tBFBBCCD2AB87AEBD14E6A168EFAA0680862814D9  ___U3CPlaneU3Ek__BackingField_1;

public:
	inline static int32_t get_offset_of_U3CPlaneSubsystemU3Ek__BackingField_0() { return static_cast<int32_t>(offsetof(PlaneRemovedEventArgs_t21E9C5879A8317E5F72263ED2235116F609E4C6A, ___U3CPlaneSubsystemU3Ek__BackingField_0)); }
	inline XRPlaneSubsystem_t3ABC3FDCBC5AC6F1CAA30E08A89EFD7F9D49D72B * get_U3CPlaneSubsystemU3Ek__BackingField_0() const { return ___U3CPlaneSubsystemU3Ek__BackingField_0; }
	inline XRPlaneSubsystem_t3ABC3FDCBC5AC6F1CAA30E08A89EFD7F9D49D72B ** get_address_of_U3CPlaneSubsystemU3Ek__BackingField_0() { return &___U3CPlaneSubsystemU3Ek__BackingField_0; }
	inline void set_U3CPlaneSubsystemU3Ek__BackingField_0(XRPlaneSubsystem_t3ABC3FDCBC5AC6F1CAA30E08A89EFD7F9D49D72B * value)
	{
		___U3CPlaneSubsystemU3Ek__BackingField_0 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___U3CPlaneSubsystemU3Ek__BackingField_0), (void*)value);
	}

	inline static int32_t get_offset_of_U3CPlaneU3Ek__BackingField_1() { return static_cast<int32_t>(offsetof(PlaneRemovedEventArgs_t21E9C5879A8317E5F72263ED2235116F609E4C6A, ___U3CPlaneU3Ek__BackingField_1)); }
	inline BoundedPlane_tBFBBCCD2AB87AEBD14E6A168EFAA0680862814D9  get_U3CPlaneU3Ek__BackingField_1() const { return ___U3CPlaneU3Ek__BackingField_1; }
	inline BoundedPlane_tBFBBCCD2AB87AEBD14E6A168EFAA0680862814D9 * get_address_of_U3CPlaneU3Ek__BackingField_1() { return &___U3CPlaneU3Ek__BackingField_1; }
	inline void set_U3CPlaneU3Ek__BackingField_1(BoundedPlane_tBFBBCCD2AB87AEBD14E6A168EFAA0680862814D9  value)
	{
		___U3CPlaneU3Ek__BackingField_1 = value;
	}
};

// Native definition for P/Invoke marshalling of UnityEngine.Experimental.XR.PlaneRemovedEventArgs
struct PlaneRemovedEventArgs_t21E9C5879A8317E5F72263ED2235116F609E4C6A_marshaled_pinvoke
{
	XRPlaneSubsystem_t3ABC3FDCBC5AC6F1CAA30E08A89EFD7F9D49D72B * ___U3CPlaneSubsystemU3Ek__BackingField_0;
	BoundedPlane_tBFBBCCD2AB87AEBD14E6A168EFAA0680862814D9  ___U3CPlaneU3Ek__BackingField_1;
};
// Native definition for COM marshalling of UnityEngine.Experimental.XR.PlaneRemovedEventArgs
struct PlaneRemovedEventArgs_t21E9C5879A8317E5F72263ED2235116F609E4C6A_marshaled_com
{
	XRPlaneSubsystem_t3ABC3FDCBC5AC6F1CAA30E08A89EFD7F9D49D72B * ___U3CPlaneSubsystemU3Ek__BackingField_0;
	BoundedPlane_tBFBBCCD2AB87AEBD14E6A168EFAA0680862814D9  ___U3CPlaneU3Ek__BackingField_1;
};

// UnityEngine.Experimental.XR.PlaneUpdatedEventArgs
struct  PlaneUpdatedEventArgs_tD63FB1655000C0BC238033545144C1FD81CED133 
{
public:
	// UnityEngine.Experimental.XR.XRPlaneSubsystem UnityEngine.Experimental.XR.PlaneUpdatedEventArgs::<PlaneSubsystem>k__BackingField
	XRPlaneSubsystem_t3ABC3FDCBC5AC6F1CAA30E08A89EFD7F9D49D72B * ___U3CPlaneSubsystemU3Ek__BackingField_0;
	// UnityEngine.Experimental.XR.BoundedPlane UnityEngine.Experimental.XR.PlaneUpdatedEventArgs::<Plane>k__BackingField
	BoundedPlane_tBFBBCCD2AB87AEBD14E6A168EFAA0680862814D9  ___U3CPlaneU3Ek__BackingField_1;

public:
	inline static int32_t get_offset_of_U3CPlaneSubsystemU3Ek__BackingField_0() { return static_cast<int32_t>(offsetof(PlaneUpdatedEventArgs_tD63FB1655000C0BC238033545144C1FD81CED133, ___U3CPlaneSubsystemU3Ek__BackingField_0)); }
	inline XRPlaneSubsystem_t3ABC3FDCBC5AC6F1CAA30E08A89EFD7F9D49D72B * get_U3CPlaneSubsystemU3Ek__BackingField_0() const { return ___U3CPlaneSubsystemU3Ek__BackingField_0; }
	inline XRPlaneSubsystem_t3ABC3FDCBC5AC6F1CAA30E08A89EFD7F9D49D72B ** get_address_of_U3CPlaneSubsystemU3Ek__BackingField_0() { return &___U3CPlaneSubsystemU3Ek__BackingField_0; }
	inline void set_U3CPlaneSubsystemU3Ek__BackingField_0(XRPlaneSubsystem_t3ABC3FDCBC5AC6F1CAA30E08A89EFD7F9D49D72B * value)
	{
		___U3CPlaneSubsystemU3Ek__BackingField_0 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___U3CPlaneSubsystemU3Ek__BackingField_0), (void*)value);
	}

	inline static int32_t get_offset_of_U3CPlaneU3Ek__BackingField_1() { return static_cast<int32_t>(offsetof(PlaneUpdatedEventArgs_tD63FB1655000C0BC238033545144C1FD81CED133, ___U3CPlaneU3Ek__BackingField_1)); }
	inline BoundedPlane_tBFBBCCD2AB87AEBD14E6A168EFAA0680862814D9  get_U3CPlaneU3Ek__BackingField_1() const { return ___U3CPlaneU3Ek__BackingField_1; }
	inline BoundedPlane_tBFBBCCD2AB87AEBD14E6A168EFAA0680862814D9 * get_address_of_U3CPlaneU3Ek__BackingField_1() { return &___U3CPlaneU3Ek__BackingField_1; }
	inline void set_U3CPlaneU3Ek__BackingField_1(BoundedPlane_tBFBBCCD2AB87AEBD14E6A168EFAA0680862814D9  value)
	{
		___U3CPlaneU3Ek__BackingField_1 = value;
	}
};

// Native definition for P/Invoke marshalling of UnityEngine.Experimental.XR.PlaneUpdatedEventArgs
struct PlaneUpdatedEventArgs_tD63FB1655000C0BC238033545144C1FD81CED133_marshaled_pinvoke
{
	XRPlaneSubsystem_t3ABC3FDCBC5AC6F1CAA30E08A89EFD7F9D49D72B * ___U3CPlaneSubsystemU3Ek__BackingField_0;
	BoundedPlane_tBFBBCCD2AB87AEBD14E6A168EFAA0680862814D9  ___U3CPlaneU3Ek__BackingField_1;
};
// Native definition for COM marshalling of UnityEngine.Experimental.XR.PlaneUpdatedEventArgs
struct PlaneUpdatedEventArgs_tD63FB1655000C0BC238033545144C1FD81CED133_marshaled_com
{
	XRPlaneSubsystem_t3ABC3FDCBC5AC6F1CAA30E08A89EFD7F9D49D72B * ___U3CPlaneSubsystemU3Ek__BackingField_0;
	BoundedPlane_tBFBBCCD2AB87AEBD14E6A168EFAA0680862814D9  ___U3CPlaneU3Ek__BackingField_1;
};

// UnityEngine.Experimental.XR.ReferencePointUpdatedEventArgs
struct  ReferencePointUpdatedEventArgs_t1B91A539846D2D040D4B0BFFD9A67B0DF30AD6BA 
{
public:
	// UnityEngine.Experimental.XR.ReferencePoint UnityEngine.Experimental.XR.ReferencePointUpdatedEventArgs::<ReferencePoint>k__BackingField
	ReferencePoint_t209849A64F7DAFF039E2519A19CA3DAE45599E7E  ___U3CReferencePointU3Ek__BackingField_0;
	// UnityEngine.Experimental.XR.TrackingState UnityEngine.Experimental.XR.ReferencePointUpdatedEventArgs::<PreviousTrackingState>k__BackingField
	int32_t ___U3CPreviousTrackingStateU3Ek__BackingField_1;
	// UnityEngine.Pose UnityEngine.Experimental.XR.ReferencePointUpdatedEventArgs::<PreviousPose>k__BackingField
	Pose_t2997DE3CB3863E4D78FCF42B46FC481818823F29  ___U3CPreviousPoseU3Ek__BackingField_2;

public:
	inline static int32_t get_offset_of_U3CReferencePointU3Ek__BackingField_0() { return static_cast<int32_t>(offsetof(ReferencePointUpdatedEventArgs_t1B91A539846D2D040D4B0BFFD9A67B0DF30AD6BA, ___U3CReferencePointU3Ek__BackingField_0)); }
	inline ReferencePoint_t209849A64F7DAFF039E2519A19CA3DAE45599E7E  get_U3CReferencePointU3Ek__BackingField_0() const { return ___U3CReferencePointU3Ek__BackingField_0; }
	inline ReferencePoint_t209849A64F7DAFF039E2519A19CA3DAE45599E7E * get_address_of_U3CReferencePointU3Ek__BackingField_0() { return &___U3CReferencePointU3Ek__BackingField_0; }
	inline void set_U3CReferencePointU3Ek__BackingField_0(ReferencePoint_t209849A64F7DAFF039E2519A19CA3DAE45599E7E  value)
	{
		___U3CReferencePointU3Ek__BackingField_0 = value;
	}

	inline static int32_t get_offset_of_U3CPreviousTrackingStateU3Ek__BackingField_1() { return static_cast<int32_t>(offsetof(ReferencePointUpdatedEventArgs_t1B91A539846D2D040D4B0BFFD9A67B0DF30AD6BA, ___U3CPreviousTrackingStateU3Ek__BackingField_1)); }
	inline int32_t get_U3CPreviousTrackingStateU3Ek__BackingField_1() const { return ___U3CPreviousTrackingStateU3Ek__BackingField_1; }
	inline int32_t* get_address_of_U3CPreviousTrackingStateU3Ek__BackingField_1() { return &___U3CPreviousTrackingStateU3Ek__BackingField_1; }
	inline void set_U3CPreviousTrackingStateU3Ek__BackingField_1(int32_t value)
	{
		___U3CPreviousTrackingStateU3Ek__BackingField_1 = value;
	}

	inline static int32_t get_offset_of_U3CPreviousPoseU3Ek__BackingField_2() { return static_cast<int32_t>(offsetof(ReferencePointUpdatedEventArgs_t1B91A539846D2D040D4B0BFFD9A67B0DF30AD6BA, ___U3CPreviousPoseU3Ek__BackingField_2)); }
	inline Pose_t2997DE3CB3863E4D78FCF42B46FC481818823F29  get_U3CPreviousPoseU3Ek__BackingField_2() const { return ___U3CPreviousPoseU3Ek__BackingField_2; }
	inline Pose_t2997DE3CB3863E4D78FCF42B46FC481818823F29 * get_address_of_U3CPreviousPoseU3Ek__BackingField_2() { return &___U3CPreviousPoseU3Ek__BackingField_2; }
	inline void set_U3CPreviousPoseU3Ek__BackingField_2(Pose_t2997DE3CB3863E4D78FCF42B46FC481818823F29  value)
	{
		___U3CPreviousPoseU3Ek__BackingField_2 = value;
	}
};


// UnityEngine.Rendering.BatchCullingContext
struct  BatchCullingContext_t63E5CFC913AA7026C975A8A79778ACC6D06E965F 
{
public:
	// Unity.Collections.NativeArray`1<UnityEngine.Plane> UnityEngine.Rendering.BatchCullingContext::cullingPlanes
	NativeArray_1_t83B803BC075802611FE185566F7FE576D1B85F52  ___cullingPlanes_0;
	// Unity.Collections.NativeArray`1<UnityEngine.Rendering.BatchVisibility> UnityEngine.Rendering.BatchCullingContext::batchVisibility
	NativeArray_1_t1D9423FECCE6FE0EBBFAB0CF4124B39FF8B66520  ___batchVisibility_1;
	// Unity.Collections.NativeArray`1<System.Int32> UnityEngine.Rendering.BatchCullingContext::visibleIndices
	NativeArray_1_tC6374EC584BF0D6DD4AD6FA0FD00C2C82F82CCAF  ___visibleIndices_2;
	// UnityEngine.Rendering.LODParameters UnityEngine.Rendering.BatchCullingContext::lodParameters
	LODParameters_t8CBE0C157487BE3E860DA9478FB46F80D3D1D960  ___lodParameters_3;

public:
	inline static int32_t get_offset_of_cullingPlanes_0() { return static_cast<int32_t>(offsetof(BatchCullingContext_t63E5CFC913AA7026C975A8A79778ACC6D06E965F, ___cullingPlanes_0)); }
	inline NativeArray_1_t83B803BC075802611FE185566F7FE576D1B85F52  get_cullingPlanes_0() const { return ___cullingPlanes_0; }
	inline NativeArray_1_t83B803BC075802611FE185566F7FE576D1B85F52 * get_address_of_cullingPlanes_0() { return &___cullingPlanes_0; }
	inline void set_cullingPlanes_0(NativeArray_1_t83B803BC075802611FE185566F7FE576D1B85F52  value)
	{
		___cullingPlanes_0 = value;
	}

	inline static int32_t get_offset_of_batchVisibility_1() { return static_cast<int32_t>(offsetof(BatchCullingContext_t63E5CFC913AA7026C975A8A79778ACC6D06E965F, ___batchVisibility_1)); }
	inline NativeArray_1_t1D9423FECCE6FE0EBBFAB0CF4124B39FF8B66520  get_batchVisibility_1() const { return ___batchVisibility_1; }
	inline NativeArray_1_t1D9423FECCE6FE0EBBFAB0CF4124B39FF8B66520 * get_address_of_batchVisibility_1() { return &___batchVisibility_1; }
	inline void set_batchVisibility_1(NativeArray_1_t1D9423FECCE6FE0EBBFAB0CF4124B39FF8B66520  value)
	{
		___batchVisibility_1 = value;
	}

	inline static int32_t get_offset_of_visibleIndices_2() { return static_cast<int32_t>(offsetof(BatchCullingContext_t63E5CFC913AA7026C975A8A79778ACC6D06E965F, ___visibleIndices_2)); }
	inline NativeArray_1_tC6374EC584BF0D6DD4AD6FA0FD00C2C82F82CCAF  get_visibleIndices_2() const { return ___visibleIndices_2; }
	inline NativeArray_1_tC6374EC584BF0D6DD4AD6FA0FD00C2C82F82CCAF * get_address_of_visibleIndices_2() { return &___visibleIndices_2; }
	inline void set_visibleIndices_2(NativeArray_1_tC6374EC584BF0D6DD4AD6FA0FD00C2C82F82CCAF  value)
	{
		___visibleIndices_2 = value;
	}

	inline static int32_t get_offset_of_lodParameters_3() { return static_cast<int32_t>(offsetof(BatchCullingContext_t63E5CFC913AA7026C975A8A79778ACC6D06E965F, ___lodParameters_3)); }
	inline LODParameters_t8CBE0C157487BE3E860DA9478FB46F80D3D1D960  get_lodParameters_3() const { return ___lodParameters_3; }
	inline LODParameters_t8CBE0C157487BE3E860DA9478FB46F80D3D1D960 * get_address_of_lodParameters_3() { return &___lodParameters_3; }
	inline void set_lodParameters_3(LODParameters_t8CBE0C157487BE3E860DA9478FB46F80D3D1D960  value)
	{
		___lodParameters_3 = value;
	}
};

// Native definition for P/Invoke marshalling of UnityEngine.Rendering.BatchCullingContext
struct BatchCullingContext_t63E5CFC913AA7026C975A8A79778ACC6D06E965F_marshaled_pinvoke
{
	NativeArray_1_t83B803BC075802611FE185566F7FE576D1B85F52  ___cullingPlanes_0;
	NativeArray_1_t1D9423FECCE6FE0EBBFAB0CF4124B39FF8B66520  ___batchVisibility_1;
	NativeArray_1_tC6374EC584BF0D6DD4AD6FA0FD00C2C82F82CCAF  ___visibleIndices_2;
	LODParameters_t8CBE0C157487BE3E860DA9478FB46F80D3D1D960  ___lodParameters_3;
};
// Native definition for COM marshalling of UnityEngine.Rendering.BatchCullingContext
struct BatchCullingContext_t63E5CFC913AA7026C975A8A79778ACC6D06E965F_marshaled_com
{
	NativeArray_1_t83B803BC075802611FE185566F7FE576D1B85F52  ___cullingPlanes_0;
	NativeArray_1_t1D9423FECCE6FE0EBBFAB0CF4124B39FF8B66520  ___batchVisibility_1;
	NativeArray_1_tC6374EC584BF0D6DD4AD6FA0FD00C2C82F82CCAF  ___visibleIndices_2;
	LODParameters_t8CBE0C157487BE3E860DA9478FB46F80D3D1D960  ___lodParameters_3;
};
#ifdef __clang__
#pragma clang diagnostic pop
#endif



static  RuntimeObject * UnresolvedVirtualCall_0 (RuntimeObject * __this, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_1 (RuntimeObject * __this, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_2 (RuntimeObject * __this, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_3 (RuntimeObject * __this, RuntimeObject * ___Object1, RuntimeObject * ___Object2, RuntimeObject * ___Object3, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeObject * UnresolvedVirtualCall_4 (RuntimeObject * __this, RuntimeObject * ___Object1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_5 (RuntimeObject * __this, RuntimeObject * ___Object1, RuntimeObject * ___Object2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_6 (RuntimeObject * __this, RuntimeObject * ___Object1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_7 (RuntimeObject * __this, RuntimeObject * ___Object1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_8 (RuntimeObject * __this, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  intptr_t UnresolvedVirtualCall_9 (RuntimeObject * __this, RuntimeObject * ___Object1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeObject * UnresolvedVirtualCall_10 (RuntimeObject * __this, RuntimeObject * ___Object1, RuntimeObject * ___Object2, int8_t ___SByte3, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeObject * UnresolvedVirtualCall_11 (RuntimeObject * __this, RuntimeObject * ___Object1, RuntimeObject * ___Object2, RuntimeObject * ___Object3, int32_t ___Int324, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int16_t UnresolvedVirtualCall_12 (RuntimeObject * __this, int16_t ___Int161, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeObject * UnresolvedVirtualCall_13 (RuntimeObject * __this, int32_t ___Int321, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeObject * UnresolvedVirtualCall_14 (RuntimeObject * __this, int32_t ___Int321, RuntimeObject * ___Object2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_15 (RuntimeObject * __this, RuntimeObject * ___Object1, RuntimeObject * ___Object2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_16 (RuntimeObject * __this, RuntimeObject * ___Object1, int32_t ___Int322, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_17 (RuntimeObject * __this, RuntimeObject * ___Object1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_18 (RuntimeObject * __this, int8_t ___SByte1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_19 (RuntimeObject * __this, RuntimeObject * ___Object1, int32_t ___Int322, int32_t ___Int323, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_20 (RuntimeObject * __this, RSAParameters_t6A568C1275FA8F8C02615666D998134DCFFB9717  ___RSAParameters1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_21 (RuntimeObject * __this, DSAParameters_tCA1A96A151F47B1904693C457243E3B919425AC6  ___DSAParameters1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RSAParameters_t6A568C1275FA8F8C02615666D998134DCFFB9717  UnresolvedVirtualCall_22 (RuntimeObject * __this, int8_t ___SByte1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeObject * UnresolvedVirtualCall_23 (RuntimeObject * __this, RuntimeObject * ___Object1, int32_t ___Int322, int32_t ___Int323, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_24 (RuntimeObject * __this, RuntimeObject * ___Object1, int32_t ___Int322, int32_t ___Int323, RuntimeObject * ___Object4, int32_t ___Int325, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_25 (RuntimeObject * __this, int32_t ___Int321, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  DSAParameters_tCA1A96A151F47B1904693C457243E3B919425AC6  UnresolvedVirtualCall_26 (RuntimeObject * __this, int8_t ___SByte1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_27 (RuntimeObject * __this, RuntimeObject * ___Object1, int32_t ___Int322, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_28 (RuntimeObject * __this, RuntimeObject * ___Object1, RuntimeObject * ___Object2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeObject * UnresolvedVirtualCall_29 (RuntimeObject * __this, RuntimeObject * ___Object1, int8_t ___SByte2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_30 (RuntimeObject * __this, RuntimeObject * ___Object1, int8_t ___SByte2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_31 (RuntimeObject * __this, int32_t ___Int321, RuntimeObject * ___Object2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_32 (RuntimeObject * __this, RuntimeObject * ___Object1, RuntimeObject * ___Object2, int32_t ___Int323, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_33 (RuntimeObject * __this, int32_t ___Int321, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  ConsoleKeyInfo_t5BE3CE05E8258CDB5404256E96AF7C22BC5DE768  UnresolvedVirtualCall_34 (RuntimeObject * __this, int8_t ___SByte1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int16_t UnresolvedVirtualCall_35 (RuntimeObject * __this, RuntimeObject * ___Object1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  uint8_t UnresolvedVirtualCall_36 (RuntimeObject * __this, RuntimeObject * ___Object1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  uint16_t UnresolvedVirtualCall_37 (RuntimeObject * __this, RuntimeObject * ___Object1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  uint32_t UnresolvedVirtualCall_38 (RuntimeObject * __this, RuntimeObject * ___Object1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int64_t UnresolvedVirtualCall_39 (RuntimeObject * __this, RuntimeObject * ___Object1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  uint64_t UnresolvedVirtualCall_40 (RuntimeObject * __this, RuntimeObject * ___Object1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  float UnresolvedVirtualCall_41 (RuntimeObject * __this, RuntimeObject * ___Object1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  double UnresolvedVirtualCall_42 (RuntimeObject * __this, RuntimeObject * ___Object1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  Decimal_t44EE9DA309A1BF848308DE4DDFC070CAE6D95EE8  UnresolvedVirtualCall_43 (RuntimeObject * __this, RuntimeObject * ___Object1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  DateTime_t349B7449FBAAFF4192636E2B7A07694DA9236132  UnresolvedVirtualCall_44 (RuntimeObject * __this, RuntimeObject * ___Object1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeObject * UnresolvedVirtualCall_45 (RuntimeObject * __this, RuntimeObject * ___Object1, RuntimeObject * ___Object2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_46 (RuntimeObject * __this, DateTime_t349B7449FBAAFF4192636E2B7A07694DA9236132  ___DateTime1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_47 (RuntimeObject * __this, RuntimeObject * ___Object1, int32_t ___Int322, int32_t ___Int323, RuntimeObject * ___Object4, int32_t ___Int325, int32_t ___Int326, int32_t ___Int327, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_48 (RuntimeObject * __this, int32_t ___Int321, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_49 (RuntimeObject * __this, int32_t ___Int321, int32_t ___Int322, int32_t ___Int323, int32_t ___Int324, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_50 (RuntimeObject * __this, int32_t ___Int321, int32_t ___Int322, int32_t ___Int323, int32_t ___Int324, int32_t ___Int325, int32_t ___Int326, int32_t ___Int327, int32_t ___Int328, RuntimeObject * ___Object9, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_51 (RuntimeObject * __this, RuntimeObject * ___Object1, int32_t ___Int322, RuntimeObject * ___Object3, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  Guid_t  UnresolvedVirtualCall_52 (RuntimeObject * __this, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_53 (RuntimeObject * __this, int32_t ___Int321, int32_t ___Int322, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  DateTime_t349B7449FBAAFF4192636E2B7A07694DA9236132  UnresolvedVirtualCall_54 (RuntimeObject * __this, int32_t ___Int321, int32_t ___Int322, int32_t ___Int323, int32_t ___Int324, int32_t ___Int325, int32_t ___Int326, int32_t ___Int327, int32_t ___Int328, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  DateTime_t349B7449FBAAFF4192636E2B7A07694DA9236132  UnresolvedVirtualCall_55 (RuntimeObject * __this, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_56 (RuntimeObject * __this, int32_t ___Int321, int32_t ___Int322, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_57 (RuntimeObject * __this, int32_t ___Int321, int32_t ___Int322, int32_t ___Int323, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_58 (RuntimeObject * __this, int32_t ___Int321, int32_t ___Int322, int32_t ___Int323, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeObject * UnresolvedVirtualCall_59 (RuntimeObject * __this, RuntimeObject * ___Object1, int32_t ___Int322, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  Nullable_1_t0D03270832B3FFDDC0E7C2D89D4A0EA25376A1EB  UnresolvedVirtualCall_60 (RuntimeObject * __this, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_61 (RuntimeObject * __this, int64_t ___Int641, int32_t ___Int322, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_62 (RuntimeObject * __this, RuntimeObject * ___Object1, int32_t ___Int322, int32_t ___Int323, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_63 (RuntimeObject * __this, RuntimeObject * ___Object1, int32_t ___Int322, RuntimeObject * ___Object3, int32_t ___Int324, int8_t ___SByte5, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int64_t UnresolvedVirtualCall_64 (RuntimeObject * __this, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int64_t UnresolvedVirtualCall_65 (RuntimeObject * __this, int64_t ___Int641, int32_t ___Int322, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  uint8_t UnresolvedVirtualCall_66 (RuntimeObject * __this, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_67 (RuntimeObject * __this, uint8_t ___Byte1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeObject * UnresolvedVirtualCall_68 (RuntimeObject * __this, RuntimeObject * ___Object1, int32_t ___Int322, int32_t ___Int323, RuntimeObject * ___Object4, RuntimeObject * ___Object5, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_69 (RuntimeObject * __this, int64_t ___Int641, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeObject * UnresolvedVirtualCall_70 (RuntimeObject * __this, RuntimeObject * ___Object1, int32_t ___Int322, int32_t ___Int323, CancellationToken_t9E956952F7F20908F2AE72EDF36D97E6C7DB63AB  ___CancellationToken4, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_71 (RuntimeObject * __this, RuntimeObject * ___Object1, int32_t ___Int322, int32_t ___Int323, RuntimeObject * ___Object4, int32_t ___Int325, int8_t ___SByte6, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_72 (RuntimeObject * __this, int16_t ___Int161, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeObject * UnresolvedVirtualCall_73 (RuntimeObject * __this, int8_t ___SByte1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  double UnresolvedVirtualCall_74 (RuntimeObject * __this, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeObject * UnresolvedVirtualCall_75 (RuntimeObject * __this, RuntimeObject * ___Object1, int8_t ___SByte2, int8_t ___SByte3, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeObject * UnresolvedVirtualCall_76 (RuntimeObject * __this, int32_t ___Int321, RuntimeObject * ___Object2, RuntimeObject * ___Object3, RuntimeObject * ___Object4, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  CustomAttributeTypedArgument_t238ACCB3A438CB5EDE4A924C637B288CCEC958E8  UnresolvedVirtualCall_77 (RuntimeObject * __this, int32_t ___Int321, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  CustomAttributeNamedArgument_t08BA731A94FD7F173551DF3098384CB9B3056E9E  UnresolvedVirtualCall_78 (RuntimeObject * __this, int32_t ___Int321, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_79 (RuntimeObject * __this, RuntimeObject * ___Object1, RuntimeObject * ___Object2, int32_t ___Int323, RuntimeObject * ___Object4, RuntimeObject * ___Object5, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeObject * UnresolvedVirtualCall_80 (RuntimeObject * __this, RuntimeObject * ___Object1, int32_t ___Int322, RuntimeObject * ___Object3, RuntimeObject * ___Object4, RuntimeObject * ___Object5, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_81 (RuntimeObject * __this, TimeSpan_tA8069278ACE8A74D6DF7D514A9CD4432433F64C4  ___TimeSpan1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeObject * UnresolvedVirtualCall_82 (RuntimeObject * __this, RuntimeObject * ___Object1, RuntimeObject * ___Object2, RuntimeObject * ___Object3, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_83 (RuntimeObject * __this, RuntimeObject * ___Object1, RuntimeObject * ___Object2, int32_t ___Int323, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_84 (RuntimeObject * __this, RuntimeObject * ___Object1, int8_t ___SByte2, int8_t ___SByte3, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  TimeSpan_tA8069278ACE8A74D6DF7D514A9CD4432433F64C4  UnresolvedVirtualCall_85 (RuntimeObject * __this, RuntimeObject * ___Object1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  TimeSpan_tA8069278ACE8A74D6DF7D514A9CD4432433F64C4  UnresolvedVirtualCall_86 (RuntimeObject * __this, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  TimeSpan_tA8069278ACE8A74D6DF7D514A9CD4432433F64C4  UnresolvedVirtualCall_87 (RuntimeObject * __this, TimeSpan_tA8069278ACE8A74D6DF7D514A9CD4432433F64C4  ___TimeSpan1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  DictionaryEntry_tB5348A26B94274FCC1DD77185BD5946E283B11A4  UnresolvedVirtualCall_88 (RuntimeObject * __this, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_89 (RuntimeObject * __this, RuntimeObject * ___Object1, StreamingContext_t2CCDC54E0E8D078AF4A50E3A8B921B828A900034  ___StreamingContext2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeObject * UnresolvedVirtualCall_90 (RuntimeObject * __this, RuntimeObject * ___Object1, StreamingContext_t2CCDC54E0E8D078AF4A50E3A8B921B828A900034  ___StreamingContext2, RuntimeObject * ___Object3, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeObject * UnresolvedVirtualCall_91 (RuntimeObject * __this, RuntimeObject * ___Object1, RuntimeObject * ___Object2, RuntimeObject * ___Object3, RuntimeObject * ___Object4, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeObject * UnresolvedVirtualCall_92 (RuntimeObject * __this, int64_t ___Int641, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeObject * UnresolvedVirtualCall_93 (RuntimeObject * __this, StreamingContext_t2CCDC54E0E8D078AF4A50E3A8B921B828A900034  ___StreamingContext1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_94 (RuntimeObject * __this, int64_t ___Int641, RuntimeObject * ___Object2, int64_t ___Int643, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int64_t UnresolvedVirtualCall_95 (RuntimeObject * __this, RuntimeObject * ___Object1, RuntimeObject * ___Object2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_96 (RuntimeObject * __this, RuntimeObject * ___Object1, RuntimeObject * ___Object2, StreamingContext_t2CCDC54E0E8D078AF4A50E3A8B921B828A900034  ___StreamingContext3, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int16_t UnresolvedVirtualCall_97 (RuntimeObject * __this, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  float UnresolvedVirtualCall_98 (RuntimeObject * __this, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  uint16_t UnresolvedVirtualCall_99 (RuntimeObject * __this, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  uint32_t UnresolvedVirtualCall_100 (RuntimeObject * __this, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  uint64_t UnresolvedVirtualCall_101 (RuntimeObject * __this, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_102 (RuntimeObject * __this, float ___Single1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_103 (RuntimeObject * __this, double ___Double1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_104 (RuntimeObject * __this, uint16_t ___UInt161, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_105 (RuntimeObject * __this, uint32_t ___UInt321, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_106 (RuntimeObject * __this, uint64_t ___UInt641, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeObject * UnresolvedVirtualCall_107 (RuntimeObject * __this, RuntimeObject * ___Object1, RuntimeObject * ___Object2, StreamingContext_t2CCDC54E0E8D078AF4A50E3A8B921B828A900034  ___StreamingContext3, RuntimeObject * ___Object4, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_108 (RuntimeObject * __this, TypedReference_t118BC3B643F75F52DB9C99D5E051299F886EB2A8  ___TypedReference1, RuntimeObject * ___Object2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_109 (RuntimeObject * __this, StreamingContext_t2CCDC54E0E8D078AF4A50E3A8B921B828A900034  ___StreamingContext1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeFieldHandle_t844BDF00E8E6FE69D9AEAA7657F09018B864F4EF  UnresolvedVirtualCall_110 (RuntimeObject * __this, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeMethodHandle_t85058E06EFF8AE085FAB91CE2B9E28E7F6FAE33F  UnresolvedVirtualCall_111 (RuntimeObject * __this, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeObject * UnresolvedVirtualCall_112 (RuntimeObject * __this, int32_t ___Int321, RuntimeObject * ___Object2, RuntimeObject * ___Object3, RuntimeObject * ___Object4, RuntimeObject * ___Object5, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeObject * UnresolvedVirtualCall_113 (RuntimeObject * __this, int32_t ___Int321, RuntimeObject * ___Object2, RuntimeObject * ___Object3, RuntimeObject * ___Object4, RuntimeObject * ___Object5, RuntimeObject * ___Object6, RuntimeObject * ___Object7, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeTypeHandle_t7B542280A22F0EC4EAC2061C29178845847A8B2D  UnresolvedVirtualCall_114 (RuntimeObject * __this, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_115 (RuntimeObject * __this, RuntimeObject * ___Object1, RuntimeObject * ___Object2, int32_t ___Int323, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_116 (RuntimeObject * __this, RuntimeObject * ___Object1, int32_t ___Int322, RuntimeObject * ___Object3, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_117 (RuntimeObject * __this, RuntimeObject * ___Object1, int32_t ___Int322, RuntimeObject * ___Object3, int32_t ___Int324, RuntimeObject * ___Object5, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_118 (RuntimeObject * __this, RuntimeObject * ___Object1, RuntimeObject * ___Object2, int32_t ___Int323, int32_t ___Int324, int32_t ___Int325, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_119 (RuntimeObject * __this, int16_t ___Int161, RuntimeObject * ___Object2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_120 (RuntimeObject * __this, RuntimeObject * ___Object1, RuntimeObject * ___Object2, RuntimeObject * ___Object3, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_121 (RuntimeObject * __this, RuntimeObject * ___Object1, int32_t ___Int322, int32_t ___Int323, int8_t ___SByte4, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_122 (RuntimeObject * __this, RuntimeObject * ___Object1, int32_t ___Int322, int8_t ___SByte3, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_123 (RuntimeObject * __this, int16_t ___Int161, int16_t ___Int162, int32_t ___Int323, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_124 (RuntimeObject * __this, int16_t ___Int161, int32_t ___Int322, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_125 (RuntimeObject * __this, RuntimeObject * ___Object1, int32_t ___Int322, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_126 (RuntimeObject * __this, RuntimeObject * ___Object1, int32_t ___Int322, RuntimeObject * ___Object3, int32_t ___Int324, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeObject * UnresolvedVirtualCall_127 (RuntimeObject * __this, RuntimeObject * ___Object1, int32_t ___Int322, RuntimeObject * ___Object3, RuntimeObject * ___Object4, RuntimeObject * ___Object5, RuntimeObject * ___Object6, RuntimeObject * ___Object7, RuntimeObject * ___Object8, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_128 (RuntimeObject * __this, RuntimeObject * ___Object1, RuntimeObject * ___Object2, int8_t ___SByte3, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_129 (RuntimeObject * __this, intptr_t ___IntPtr1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_130 (RuntimeObject * __this, RuntimeObject * ___Object1, int8_t ___SByte2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeObject * UnresolvedVirtualCall_131 (RuntimeObject * __this, RuntimeObject * ___Object1, RuntimeObject * ___Object2, RuntimeObject * ___Object3, RuntimeObject * ___Object4, int32_t ___Int325, int32_t ___Int326, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_132 (RuntimeObject * __this, int32_t ___Int321, int8_t ___SByte2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_133 (RuntimeObject * __this, TimeSpan_tA8069278ACE8A74D6DF7D514A9CD4432433F64C4  ___TimeSpan1, int8_t ___SByte2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeObject * UnresolvedVirtualCall_134 (RuntimeObject * __this, int32_t ___Int321, RuntimeObject * ___Object2, int32_t ___Int323, RuntimeObject * ___Object4, RuntimeObject * ___Object5, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeObject * UnresolvedVirtualCall_135 (RuntimeObject * __this, RuntimeObject * ___Object1, int32_t ___Int322, RuntimeObject * ___Object3, int32_t ___Int324, RuntimeObject * ___Object5, RuntimeObject * ___Object6, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeObject * UnresolvedVirtualCall_136 (RuntimeObject * __this, RuntimeObject * ___Object1, int32_t ___Int322, RuntimeObject * ___Object3, RuntimeObject * ___Object4, RuntimeObject * ___Object5, RuntimeObject * ___Object6, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_137 (RuntimeObject * __this, intptr_t ___IntPtr1, intptr_t ___IntPtr2, intptr_t ___IntPtr3, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_138 (RuntimeObject * __this, RuntimeObject * ___Object1, RuntimeObject * ___Object2, int8_t ___SByte3, RuntimeObject * ___Object4, int8_t ___SByte5, RuntimeObject * ___Object6, RuntimeObject * ___Object7, RuntimeObject * ___Object8, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_139 (RuntimeObject * __this, RuntimeObject * ___Object1, RuntimeObject * ___Object2, RuntimeObject * ___Object3, int32_t ___Int324, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  ValueTuple_2_t446756A8057B54D18CAD5BA1D73699DA4B40A264  UnresolvedVirtualCall_140 (RuntimeObject * __this, RuntimeObject * ___Object1, int32_t ___Int322, int32_t ___Int323, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeObject * UnresolvedVirtualCall_141 (RuntimeObject * __this, int8_t ___SByte1, RuntimeObject * ___Object2, int32_t ___Int323, RuntimeObject * ___Object4, RuntimeObject * ___Object5, int8_t ___SByte6, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeObject * UnresolvedVirtualCall_142 (RuntimeObject * __this, RuntimeObject * ___Object1, int8_t ___SByte2, RuntimeObject * ___Object3, RuntimeObject * ___Object4, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeObject * UnresolvedVirtualCall_143 (RuntimeObject * __this, RuntimeObject * ___Object1, int8_t ___SByte2, RuntimeObject * ___Object3, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_144 (RuntimeObject * __this, RuntimeObject * ___Object1, RuntimeObject * ___Object2, RuntimeObject * ___Object3, RuntimeObject * ___Object4, RuntimeObject * ___Object5, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  unitytls_errorstate_t64FA817A583B1CD3CB1AFCFF9606F1F2782ABBE6  UnresolvedVirtualCall_145 (RuntimeObject * __this, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_146 (RuntimeObject * __this, RuntimeObject * ___Object1, uint32_t ___UInt322, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  unitytls_key_ref_tE908606656A7C49CA1EB734722E4C3DED7CE6E5B  UnresolvedVirtualCall_147 (RuntimeObject * __this, RuntimeObject * ___Object1, RuntimeObject * ___Object2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeObject * UnresolvedVirtualCall_148 (RuntimeObject * __this, RuntimeObject * ___Object1, intptr_t ___IntPtr2, RuntimeObject * ___Object3, intptr_t ___IntPtr4, RuntimeObject * ___Object5, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_149 (RuntimeObject * __this, RuntimeObject * ___Object1, intptr_t ___IntPtr2, RuntimeObject * ___Object3, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeObject * UnresolvedVirtualCall_150 (RuntimeObject * __this, unitytls_tlsctx_protocolrange_t36243D72F83DAD47C95928314F58026DE8D38F47  ___unitytls_tlsctx_protocolrange1, unitytls_tlsctx_callbacks_t7BB5F622E014A8EC300C578657E2B0550DA828B2  ___unitytls_tlsctx_callbacks2, RuntimeObject * ___Object3, intptr_t ___IntPtr4, RuntimeObject * ___Object5, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeObject * UnresolvedVirtualCall_151 (RuntimeObject * __this, unitytls_tlsctx_protocolrange_t36243D72F83DAD47C95928314F58026DE8D38F47  ___unitytls_tlsctx_protocolrange1, unitytls_tlsctx_callbacks_t7BB5F622E014A8EC300C578657E2B0550DA828B2  ___unitytls_tlsctx_callbacks2, uint64_t ___UInt643, uint64_t ___UInt644, RuntimeObject * ___Object5, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  uint32_t UnresolvedVirtualCall_152 (RuntimeObject * __this, RuntimeObject * ___Object1, RuntimeObject * ___Object2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  intptr_t UnresolvedVirtualCall_153 (RuntimeObject * __this, RuntimeObject * ___Object1, RuntimeObject * ___Object2, intptr_t ___IntPtr3, RuntimeObject * ___Object4, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_154 (RuntimeObject * __this, RuntimeObject * ___Object1, unitytls_x509list_ref_tF01A6BF5ADA9C454E6B975D2669AF22D27555BF6  ___unitytls_x509list_ref2, RuntimeObject * ___Object3, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_155 (RuntimeObject * __this, RuntimeObject * ___Object1, RuntimeObject * ___Object2, RuntimeObject * ___Object3, RuntimeObject * ___Object4, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_156 (RuntimeObject * __this, RuntimeObject * ___Object1, RuntimeObject * ___Object2, intptr_t ___IntPtr3, RuntimeObject * ___Object4, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  intptr_t UnresolvedVirtualCall_157 (RuntimeObject * __this, unitytls_x509_ref_tE1ED17887226610A1328A57FF787396C9457E7B7  ___unitytls_x509_ref1, RuntimeObject * ___Object2, intptr_t ___IntPtr3, RuntimeObject * ___Object4, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_158 (RuntimeObject * __this, RuntimeObject * ___Object1, unitytls_x509_ref_tE1ED17887226610A1328A57FF787396C9457E7B7  ___unitytls_x509_ref2, RuntimeObject * ___Object3, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  unitytls_x509list_ref_tF01A6BF5ADA9C454E6B975D2669AF22D27555BF6  UnresolvedVirtualCall_159 (RuntimeObject * __this, RuntimeObject * ___Object1, RuntimeObject * ___Object2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  unitytls_x509_ref_tE1ED17887226610A1328A57FF787396C9457E7B7  UnresolvedVirtualCall_160 (RuntimeObject * __this, unitytls_x509list_ref_tF01A6BF5ADA9C454E6B975D2669AF22D27555BF6  ___unitytls_x509list_ref1, intptr_t ___IntPtr2, RuntimeObject * ___Object3, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  uint32_t UnresolvedVirtualCall_161 (RuntimeObject * __this, unitytls_x509list_ref_tF01A6BF5ADA9C454E6B975D2669AF22D27555BF6  ___unitytls_x509list_ref1, RuntimeObject * ___Object2, intptr_t ___IntPtr3, RuntimeObject * ___Object4, RuntimeObject * ___Object5, RuntimeObject * ___Object6, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  uint32_t UnresolvedVirtualCall_162 (RuntimeObject * __this, unitytls_x509list_ref_tF01A6BF5ADA9C454E6B975D2669AF22D27555BF6  ___unitytls_x509list_ref1, unitytls_x509list_ref_tF01A6BF5ADA9C454E6B975D2669AF22D27555BF6  ___unitytls_x509list_ref2, RuntimeObject * ___Object3, intptr_t ___IntPtr4, RuntimeObject * ___Object5, RuntimeObject * ___Object6, RuntimeObject * ___Object7, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_163 (RuntimeObject * __this, RuntimeObject * ___Object1, RuntimeObject * ___Object2, RuntimeObject * ___Object3, intptr_t ___IntPtr4, RuntimeObject * ___Object5, intptr_t ___IntPtr6, RuntimeObject * ___Object7, RuntimeObject * ___Object8, RuntimeObject * ___Object9, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_164 (RuntimeObject * __this, RuntimeObject * ___Object1, RuntimeObject * ___Object2, RuntimeObject * ___Object3, intptr_t ___IntPtr4, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  uint32_t UnresolvedVirtualCall_165 (RuntimeObject * __this, RuntimeObject * ___Object1, unitytls_x509list_ref_tF01A6BF5ADA9C454E6B975D2669AF22D27555BF6  ___unitytls_x509list_ref2, RuntimeObject * ___Object3, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  uint32_t UnresolvedVirtualCall_166 (RuntimeObject * __this, RuntimeObject * ___Object1, unitytls_x509_ref_tE1ED17887226610A1328A57FF787396C9457E7B7  ___unitytls_x509_ref2, uint32_t ___UInt323, RuntimeObject * ___Object4, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_167 (RuntimeObject * __this, intptr_t ___IntPtr1, int32_t ___Int322, intptr_t ___IntPtr3, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeObject * UnresolvedVirtualCall_168 (RuntimeObject * __this, RuntimeObject * ___Object1, RuntimeObject * ___Object2, RuntimeObject * ___Object3, RuntimeObject * ___Object4, RuntimeObject * ___Object5, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeObject * UnresolvedVirtualCall_169 (RuntimeObject * __this, RuntimeObject * ___Object1, RuntimeObject * ___Object2, int8_t ___SByte3, RuntimeObject * ___Object4, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_170 (RuntimeObject * __this, RuntimeObject * ___Object1, int8_t ___SByte2, int32_t ___Int323, int8_t ___SByte4, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  ArraySegment_1_t5B17204266E698CC035E2A7F6435A4F78286D0FA  UnresolvedVirtualCall_171 (RuntimeObject * __this, int32_t ___Int321, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_172 (RuntimeObject * __this, RuntimeObject * ___Object1, int32_t ___Int322, RuntimeObject * ___Object3, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_173 (RuntimeObject * __this, int32_t ___Int321, int32_t ___Int322, int32_t ___Int323, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_174 (RuntimeObject * __this, RuntimeObject * ___Object1, RuntimeObject * ___Object2, int32_t ___Int323, int32_t ___Int324, int32_t ___Int325, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_175 (RuntimeObject * __this, CullingGroupEvent_tC36FFE61D0A4E7B31F575A1FCAEE05AC41FACA85  ___CullingGroupEvent1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_176 (RuntimeObject * __this, int32_t ___Int321, RuntimeObject * ___Object2, RuntimeObject * ___Object3, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_177 (RuntimeObject * __this, int32_t ___Int321, RuntimeObject * ___Object2, RuntimeObject * ___Object3, RuntimeObject * ___Object4, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_178 (RuntimeObject * __this, RuntimeObject * ___Object1, NativeArray_1_t8C7BA311FA83977B4520E39644CB5AD8F1E9E0FE  ___NativeArray_12, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_179 (RuntimeObject * __this, Guid_t  ___Guid1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_180 (RuntimeObject * __this, Guid_t  ___Guid1, RuntimeObject * ___Object2, int32_t ___Int323, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  Playable_t4ABB910C374FCAB6B926DA4D34A85857A59950D0  UnresolvedVirtualCall_181 (RuntimeObject * __this, PlayableGraph_tEC38BBCA59BDD496F75037F220984D41339AB8BA  ___PlayableGraph1, RuntimeObject * ___Object2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  PlayableOutput_t5E024C3D28C983782CD4FDB2FA5AD23998D21345  UnresolvedVirtualCall_182 (RuntimeObject * __this, PlayableGraph_tEC38BBCA59BDD496F75037F220984D41339AB8BA  ___PlayableGraph1, RuntimeObject * ___Object2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  JobHandle_tDA498A2E49AEDE014468F416A8A98A6B258D73D1  UnresolvedVirtualCall_183 (RuntimeObject * __this, RuntimeObject * ___Object1, BatchCullingContext_t63E5CFC913AA7026C975A8A79778ACC6D06E965F  ___BatchCullingContext2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_184 (RuntimeObject * __this, ScriptableRenderContext_t7A3C889E3516E8C79C1C0327D33ED9601D163A2B  ___ScriptableRenderContext1, RuntimeObject * ___Object2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_185 (RuntimeObject * __this, PhraseRecognizedEventArgs_t5045E5956BF185A7C661A2B56466E9C6101BAFAD  ___PhraseRecognizedEventArgs1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_186 (RuntimeObject * __this, PhotoCaptureResult_tCA8987284B3260E035A45A98C6B46485952A3900  ___PhotoCaptureResult1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_187 (RuntimeObject * __this, PhotoCaptureResult_tCA8987284B3260E035A45A98C6B46485952A3900  ___PhotoCaptureResult1, RuntimeObject * ___Object2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_188 (RuntimeObject * __this, VideoCaptureResult_t68444D73B1DD8952CF970D983DFF25517C7C516A  ___VideoCaptureResult1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_189 (RuntimeObject * __this, float ___Single1, float ___Single2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  Rect_t35B976DE901B5423C11705E156938EA27AB402CE  UnresolvedVirtualCall_190 (RuntimeObject * __this, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_191 (RuntimeObject * __this, TimeSpan_tA8069278ACE8A74D6DF7D514A9CD4432433F64C4  ___TimeSpan1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_192 (RuntimeObject * __this, int32_t ___Int321, RuntimeObject * ___Object2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_193 (RuntimeObject * __this, int32_t ___Int321, int32_t ___Int322, RuntimeObject * ___Object3, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_194 (RuntimeObject * __this, ArraySegment_1_t5B17204266E698CC035E2A7F6435A4F78286D0FA  ___ArraySegment_11, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_195 (RuntimeObject * __this, NetworkMessage_t7F8AC96151454587E6108E42F7EC0C2DD38A99BB  ___NetworkMessage1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeObject * UnresolvedVirtualCall_196 (RuntimeObject * __this, Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  ___Vector31, Guid_t  ___Guid2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeObject * UnresolvedVirtualCall_197 (RuntimeObject * __this, int32_t ___Int321, RuntimeObject * ___Object2, CancellationToken_t9E956952F7F20908F2AE72EDF36D97E6C7DB63AB  ___CancellationToken3, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeObject * UnresolvedVirtualCall_198 (RuntimeObject * __this, ArraySegment_1_t5B17204266E698CC035E2A7F6435A4F78286D0FA  ___ArraySegment_11, CancellationToken_t9E956952F7F20908F2AE72EDF36D97E6C7DB63AB  ___CancellationToken2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeObject * UnresolvedVirtualCall_199 (RuntimeObject * __this, ArraySegment_1_t5B17204266E698CC035E2A7F6435A4F78286D0FA  ___ArraySegment_11, int32_t ___Int322, int8_t ___SByte3, CancellationToken_t9E956952F7F20908F2AE72EDF36D97E6C7DB63AB  ___CancellationToken4, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeObject * UnresolvedVirtualCall_200 (RuntimeObject * __this, RuntimeObject * ___Object1, CancellationToken_t9E956952F7F20908F2AE72EDF36D97E6C7DB63AB  ___CancellationToken2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeObject * UnresolvedVirtualCall_201 (RuntimeObject * __this, RuntimeObject * ___Object1, RuntimeObject * ___Object2, CancellationToken_t9E956952F7F20908F2AE72EDF36D97E6C7DB63AB  ___CancellationToken3, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  KeyValuePair_2_t86464C52F9602337EAC68825E6BE06951D7530CE  UnresolvedVirtualCall_202 (RuntimeObject * __this, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  uint32_t UnresolvedVirtualCall_203 (RuntimeObject * __this, uint32_t ___UInt321, intptr_t ___IntPtr2, uint32_t ___UInt323, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_204 (RuntimeObject * __this, RuntimeObject * ___Object1, RectInt_t595A63F7EE2BC91A4D2DE5403C5FE94D3C3A6F7A  ___RectInt2, int8_t ___SByte3, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_205 (RuntimeObject * __this, RuntimeObject * ___Object1, RuntimeObject * ___Object2, RectInt_t595A63F7EE2BC91A4D2DE5403C5FE94D3C3A6F7A  ___RectInt3, int8_t ___SByte4, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_206 (RuntimeObject * __this, Vector3Int_tA843C5F8C2EB42492786C5AF82C3E1F4929942B4  ___Vector3Int1, RuntimeObject * ___Object2, RuntimeObject * ___Object3, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_207 (RuntimeObject * __this, Vector3Int_tA843C5F8C2EB42492786C5AF82C3E1F4929942B4  ___Vector3Int1, RuntimeObject * ___Object2, RuntimeObject * ___Object3, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  UnresolvedVirtualCall_208 (RuntimeObject * __this, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  Touch_tAACD32535FF3FE5DD91125E0B6987B93C68D2DE8  UnresolvedVirtualCall_209 (RuntimeObject * __this, int32_t ___Int321, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeObject * UnresolvedVirtualCall_210 (RuntimeObject * __this, float ___Single1, float ___Single2, float ___Single3, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_211 (RuntimeObject * __this, int32_t ___Int321, int8_t ___SByte2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_212 (RuntimeObject * __this, Color_t119BCA590009762C7223FDD3AF9706653AC84ED2  ___Color1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  Color_t119BCA590009762C7223FDD3AF9706653AC84ED2  UnresolvedVirtualCall_213 (RuntimeObject * __this, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_214 (RuntimeObject * __this, Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  ___Vector21, RuntimeObject * ___Object2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_215 (RuntimeObject * __this, Color_t119BCA590009762C7223FDD3AF9706653AC84ED2  ___Color1, float ___Single2, int8_t ___SByte3, int8_t ___SByte4, int8_t ___SByte5, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  UILineInfo_t0AF27251CA07CEE2BC0C1FEF752245596B8033E6  UnresolvedVirtualCall_216 (RuntimeObject * __this, int32_t ___Int321, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  UICharInfo_tB4C92043A686A600D36A92E3108F173C499E318A  UnresolvedVirtualCall_217 (RuntimeObject * __this, int32_t ___Int321, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_218 (RuntimeObject * __this, Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  ___Vector21, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int16_t UnresolvedVirtualCall_219 (RuntimeObject * __this, RuntimeObject * ___Object1, int32_t ___Int322, int16_t ___Int163, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_220 (RuntimeObject * __this, Rect_t35B976DE901B5423C11705E156938EA27AB402CE  ___Rect1, int8_t ___SByte2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeObject * UnresolvedVirtualCall_221 (RuntimeObject * __this, Ray_tE2163D4CB3E6B267E29F8ABE41684490E4A614B2  ___Ray1, float ___Single2, int32_t ___Int323, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_222 (RuntimeObject * __this, Ray_tE2163D4CB3E6B267E29F8ABE41684490E4A614B2  ___Ray1, RuntimeObject * ___Object2, float ___Single3, int32_t ___Int324, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RaycastHit2D_t5E8A7F96317BAF2033362FC780F4D72DC72764BE  UnresolvedVirtualCall_223 (RuntimeObject * __this, Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  ___Vector21, Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  ___Vector22, float ___Single3, int32_t ___Int324, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_224 (RuntimeObject * __this, Ray_tE2163D4CB3E6B267E29F8ABE41684490E4A614B2  ___Ray1, RuntimeObject * ___Object2, float ___Single3, int32_t ___Int324, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_225 (RuntimeObject * __this, float ___Single1, int32_t ___Int322, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_226 (RuntimeObject * __this, Color_t119BCA590009762C7223FDD3AF9706653AC84ED2  ___Color1, float ___Single2, int8_t ___SByte3, int8_t ___SByte4, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_227 (RuntimeObject * __this, float ___Single1, int8_t ___SByte2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  UIVertex_t0583C35B730B218B542E80203F5F4BC6F1E9E577  UnresolvedVirtualCall_228 (RuntimeObject * __this, int32_t ___Int321, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_229 (RuntimeObject * __this, float ___Single1, float ___Single2, int8_t ___SByte3, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  float UnresolvedVirtualCall_230 (RuntimeObject * __this, RuntimeObject * ___Object1, float ___Single2, float ___Single3, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  YogaSize_t0F2077727A4CBD4C36F3DC0CBE1FB0A67D9EAD23  UnresolvedVirtualCall_231 (RuntimeObject * __this, RuntimeObject * ___Object1, float ___Single2, int32_t ___Int323, float ___Single4, int32_t ___Int325, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_232 (RuntimeObject * __this, int32_t ___Int321, int64_t ___Int642, int64_t ___Int643, int8_t ___SByte4, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_233 (RuntimeObject * __this, RuntimeObject * ___Object1, int64_t ___Int642, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_234 (RuntimeObject * __this, RuntimeObject * ___Object1, double ___Double2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_235 (RuntimeObject * __this, RuntimeObject * ___Object1, RuntimeObject * ___Object2, float ___Single3, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_236 (RuntimeObject * __this, int8_t ___SByte1, int8_t ___SByte2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_237 (RuntimeObject * __this, int16_t ___Int161, int16_t ___Int162, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_238 (RuntimeObject * __this, float ___Single1, float ___Single2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_239 (RuntimeObject * __this, ColorBlock_t93B54DF6E8D65D24CEA9726CA745E48C53E3B1EA  ___ColorBlock1, ColorBlock_t93B54DF6E8D65D24CEA9726CA745E48C53E3B1EA  ___ColorBlock2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_240 (RuntimeObject * __this, Navigation_t761250C05C09773B75F5E0D52DDCBBFE60288A07  ___Navigation1, Navigation_t761250C05C09773B75F5E0D52DDCBBFE60288A07  ___Navigation2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_241 (RuntimeObject * __this, SpriteState_t58B9DD66A79CD69AB4CFC3AD0C41E45DC2192C0A  ___SpriteState1, SpriteState_t58B9DD66A79CD69AB4CFC3AD0C41E45DC2192C0A  ___SpriteState2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_242 (RuntimeObject * __this, RuntimeObject * ___Object1, PendingPlayer_t40459CEDE375BC08A48A97694355F960BF84967F  ___PendingPlayer2, int32_t ___Int323, int32_t ___Int324, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_243 (RuntimeObject * __this, RuntimeObject * ___Object1, Change_tA73B83D716F0F54FF7C49B3C2789779480325A58  ___Change2, int32_t ___Int323, int32_t ___Int324, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_244 (RuntimeObject * __this, RuntimeObject * ___Object1, Change_t70C8E48E2782F7DA0BC9C1518C4FAD99E6DB80DA  ___Change2, int32_t ___Int323, int32_t ___Int324, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_245 (RuntimeObject * __this, RuntimeObject * ___Object1, uint8_t ___Byte2, int32_t ___Int323, int32_t ___Int324, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_246 (RuntimeObject * __this, RuntimeObject * ___Object1, KeyValuePair_2_t5DDBBB9A3C8CBE3A4A39721E8F0A10AEBF13737B  ___KeyValuePair_22, int32_t ___Int323, int32_t ___Int324, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_247 (RuntimeObject * __this, RuntimeObject * ___Object1, KeyValuePair_2_t23481547E419E16E3B96A303578C1EB685C99EEE  ___KeyValuePair_22, int32_t ___Int323, int32_t ___Int324, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_248 (RuntimeObject * __this, RuntimeObject * ___Object1, int32_t ___Int322, int32_t ___Int323, int32_t ___Int324, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_249 (RuntimeObject * __this, RuntimeObject * ___Object1, RuntimeObject * ___Object2, int32_t ___Int323, int32_t ___Int324, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_250 (RuntimeObject * __this, RuntimeObject * ___Object1, OrderBlock_t3B2BBCE8320FAEC3DB605F7DC9AB641102F53727  ___OrderBlock2, int32_t ___Int323, int32_t ___Int324, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_251 (RuntimeObject * __this, RuntimeObject * ___Object1, Color32_t23ABC4AE0E0BDFD2E22EE1FA0DA3904FFE5F6E23  ___Color322, int32_t ___Int323, int32_t ___Int324, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_252 (RuntimeObject * __this, RuntimeObject * ___Object1, Color_t119BCA590009762C7223FDD3AF9706653AC84ED2  ___Color2, int32_t ___Int323, int32_t ___Int324, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_253 (RuntimeObject * __this, RuntimeObject * ___Object1, RaycastResult_t991BCED43A91EDD8580F39631DA07B1F88C58B91  ___RaycastResult2, int32_t ___Int323, int32_t ___Int324, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_254 (RuntimeObject * __this, RuntimeObject * ___Object1, RaycastHit2D_t5E8A7F96317BAF2033362FC780F4D72DC72764BE  ___RaycastHit2D2, int32_t ___Int323, int32_t ___Int324, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_255 (RuntimeObject * __this, RuntimeObject * ___Object1, RenderTargetIdentifier_tB7480EE944FC70E0AB7D499DB17D119EB65B0F5B  ___RenderTargetIdentifier2, int32_t ___Int323, int32_t ___Int324, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_256 (RuntimeObject * __this, RuntimeObject * ___Object1, UICharInfo_tB4C92043A686A600D36A92E3108F173C499E318A  ___UICharInfo2, int32_t ___Int323, int32_t ___Int324, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_257 (RuntimeObject * __this, RuntimeObject * ___Object1, UILineInfo_t0AF27251CA07CEE2BC0C1FEF752245596B8033E6  ___UILineInfo2, int32_t ___Int323, int32_t ___Int324, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_258 (RuntimeObject * __this, RuntimeObject * ___Object1, UIVertex_t0583C35B730B218B542E80203F5F4BC6F1E9E577  ___UIVertex2, int32_t ___Int323, int32_t ___Int324, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_259 (RuntimeObject * __this, RuntimeObject * ___Object1, WorkRequest_t0247B62D135204EAA95FC0B2EC829CB27B433F94  ___WorkRequest2, int32_t ___Int323, int32_t ___Int324, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_260 (RuntimeObject * __this, RuntimeObject * ___Object1, Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  ___Vector22, int32_t ___Int323, int32_t ___Int324, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_261 (RuntimeObject * __this, RuntimeObject * ___Object1, Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  ___Vector32, int32_t ___Int323, int32_t ___Int324, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_262 (RuntimeObject * __this, RuntimeObject * ___Object1, Vector4_tD148D6428C3F8FF6CD998F82090113C2B490B76E  ___Vector42, int32_t ___Int323, int32_t ___Int324, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_263 (RuntimeObject * __this, uint32_t ___UInt321, uint32_t ___UInt322, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_264 (RuntimeObject * __this, Color_t119BCA590009762C7223FDD3AF9706653AC84ED2  ___Color1, Color_t119BCA590009762C7223FDD3AF9706653AC84ED2  ___Color2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_265 (RuntimeObject * __this, Quaternion_t319F3319A7D43FFA5D819AD6C0A98851F0095357  ___Quaternion1, Quaternion_t319F3319A7D43FFA5D819AD6C0A98851F0095357  ___Quaternion2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_266 (RuntimeObject * __this, uint8_t ___Byte1, int32_t ___Int322, int32_t ___Int323, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_267 (RuntimeObject * __this, uint8_t ___Byte1, int32_t ___Int322, RuntimeObject * ___Object3, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_268 (RuntimeObject * __this, int32_t ___Int321, int32_t ___Int322, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_269 (RuntimeObject * __this, AsyncLocalValueChangedArgs_1_t64BF6800935406CA808E9821DF12DBB72A71640D  ___AsyncLocalValueChangedArgs_11, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_270 (RuntimeObject * __this, FrameReceivedEventArgs_t4637B6D2FC28197602B18C1815C4A778645479DD  ___FrameReceivedEventArgs1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_271 (RuntimeObject * __this, GestureHoldEvent_tE6ACA548C0C4D8F8C3B197066BD5E7B46A6B576A  ___GestureHoldEvent1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_272 (RuntimeObject * __this, GestureManipulationEvent_t80F0163CC6437F2E1BC5B773E9599AF499D08E4F  ___GestureManipulationEvent1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_273 (RuntimeObject * __this, GestureNavigationEvent_t3718B4FDF2C71A07DFAB23CE2E27D1D022417866  ___GestureNavigationEvent1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_274 (RuntimeObject * __this, GestureRecognitionEvent_tBB5EFD6FF7697E5F8FDFB64BA9A51003282AEE68  ___GestureRecognitionEvent1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_275 (RuntimeObject * __this, GestureTappedEvent_t793E61A597E40572998004754809BBF49D738535  ___GestureTappedEvent1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_276 (RuntimeObject * __this, MeshGenerationResult_t24F21E71F8F697D7D216BA4F3F064FB5434E6056  ___MeshGenerationResult1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_277 (RuntimeObject * __this, PlaneAddedEventArgs_t06BF8697BA4D8CD3A8C9AB8DF51F8D01D2910002  ___PlaneAddedEventArgs1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_278 (RuntimeObject * __this, PlaneRemovedEventArgs_t21E9C5879A8317E5F72263ED2235116F609E4C6A  ___PlaneRemovedEventArgs1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_279 (RuntimeObject * __this, PlaneUpdatedEventArgs_tD63FB1655000C0BC238033545144C1FD81CED133  ___PlaneUpdatedEventArgs1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_280 (RuntimeObject * __this, PointCloudUpdatedEventArgs_tE7E1E32A6042806B927B110250C0D4FE755C6B07  ___PointCloudUpdatedEventArgs1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_281 (RuntimeObject * __this, ReferencePointUpdatedEventArgs_t1B91A539846D2D040D4B0BFFD9A67B0DF30AD6BA  ___ReferencePointUpdatedEventArgs1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_282 (RuntimeObject * __this, SessionTrackingStateChangedEventArgs_tE4B00077E5AAE143593A0BB3FA2C57237525E7BA  ___SessionTrackingStateChangedEventArgs1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_283 (RuntimeObject * __this, XRNodeState_t927C248D649ED31F587DFE078E3FF180D98F7C0A  ___XRNodeState1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_284 (RuntimeObject * __this, int32_t ___Int321, ArraySegment_1_t5B17204266E698CC035E2A7F6435A4F78286D0FA  ___ArraySegment_12, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_285 (RuntimeObject * __this, int8_t ___SByte1, int8_t ___SByte2, int32_t ___Int323, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  KeyValuePair_2_t23481547E419E16E3B96A303578C1EB685C99EEE  UnresolvedVirtualCall_286 (RuntimeObject * __this, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  Message_tF83CFFEC28E30B669B3165790493DAE2F48D6B4E  UnresolvedVirtualCall_287 (RuntimeObject * __this, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_288 (RuntimeObject * __this, PendingPlayer_t40459CEDE375BC08A48A97694355F960BF84967F  ___PendingPlayer1, PendingPlayer_t40459CEDE375BC08A48A97694355F960BF84967F  ___PendingPlayer2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_289 (RuntimeObject * __this, Change_tA73B83D716F0F54FF7C49B3C2789779480325A58  ___Change1, Change_tA73B83D716F0F54FF7C49B3C2789779480325A58  ___Change2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_290 (RuntimeObject * __this, Change_t70C8E48E2782F7DA0BC9C1518C4FAD99E6DB80DA  ___Change1, Change_t70C8E48E2782F7DA0BC9C1518C4FAD99E6DB80DA  ___Change2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_291 (RuntimeObject * __this, uint8_t ___Byte1, uint8_t ___Byte2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_292 (RuntimeObject * __this, KeyValuePair_2_t5DDBBB9A3C8CBE3A4A39721E8F0A10AEBF13737B  ___KeyValuePair_21, KeyValuePair_2_t5DDBBB9A3C8CBE3A4A39721E8F0A10AEBF13737B  ___KeyValuePair_22, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_293 (RuntimeObject * __this, KeyValuePair_2_t23481547E419E16E3B96A303578C1EB685C99EEE  ___KeyValuePair_21, KeyValuePair_2_t23481547E419E16E3B96A303578C1EB685C99EEE  ___KeyValuePair_22, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_294 (RuntimeObject * __this, uint64_t ___UInt641, uint64_t ___UInt642, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_295 (RuntimeObject * __this, OrderBlock_t3B2BBCE8320FAEC3DB605F7DC9AB641102F53727  ___OrderBlock1, OrderBlock_t3B2BBCE8320FAEC3DB605F7DC9AB641102F53727  ___OrderBlock2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_296 (RuntimeObject * __this, Color32_t23ABC4AE0E0BDFD2E22EE1FA0DA3904FFE5F6E23  ___Color321, Color32_t23ABC4AE0E0BDFD2E22EE1FA0DA3904FFE5F6E23  ___Color322, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_297 (RuntimeObject * __this, Color_t119BCA590009762C7223FDD3AF9706653AC84ED2  ___Color1, Color_t119BCA590009762C7223FDD3AF9706653AC84ED2  ___Color2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_298 (RuntimeObject * __this, RaycastResult_t991BCED43A91EDD8580F39631DA07B1F88C58B91  ___RaycastResult1, RaycastResult_t991BCED43A91EDD8580F39631DA07B1F88C58B91  ___RaycastResult2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_299 (RuntimeObject * __this, RaycastHit2D_t5E8A7F96317BAF2033362FC780F4D72DC72764BE  ___RaycastHit2D1, RaycastHit2D_t5E8A7F96317BAF2033362FC780F4D72DC72764BE  ___RaycastHit2D2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_300 (RuntimeObject * __this, RaycastHit_t19695F18F9265FE5425062BBA6A4D330480538C3  ___RaycastHit1, RaycastHit_t19695F18F9265FE5425062BBA6A4D330480538C3  ___RaycastHit2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_301 (RuntimeObject * __this, RenderTargetIdentifier_tB7480EE944FC70E0AB7D499DB17D119EB65B0F5B  ___RenderTargetIdentifier1, RenderTargetIdentifier_tB7480EE944FC70E0AB7D499DB17D119EB65B0F5B  ___RenderTargetIdentifier2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_302 (RuntimeObject * __this, UICharInfo_tB4C92043A686A600D36A92E3108F173C499E318A  ___UICharInfo1, UICharInfo_tB4C92043A686A600D36A92E3108F173C499E318A  ___UICharInfo2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_303 (RuntimeObject * __this, UILineInfo_t0AF27251CA07CEE2BC0C1FEF752245596B8033E6  ___UILineInfo1, UILineInfo_t0AF27251CA07CEE2BC0C1FEF752245596B8033E6  ___UILineInfo2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_304 (RuntimeObject * __this, UIVertex_t0583C35B730B218B542E80203F5F4BC6F1E9E577  ___UIVertex1, UIVertex_t0583C35B730B218B542E80203F5F4BC6F1E9E577  ___UIVertex2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_305 (RuntimeObject * __this, WorkRequest_t0247B62D135204EAA95FC0B2EC829CB27B433F94  ___WorkRequest1, WorkRequest_t0247B62D135204EAA95FC0B2EC829CB27B433F94  ___WorkRequest2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_306 (RuntimeObject * __this, Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  ___Vector21, Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  ___Vector22, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_307 (RuntimeObject * __this, Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  ___Vector31, Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  ___Vector32, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_308 (RuntimeObject * __this, Vector4_tD148D6428C3F8FF6CD998F82090113C2B490B76E  ___Vector41, Vector4_tD148D6428C3F8FF6CD998F82090113C2B490B76E  ___Vector42, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_309 (RuntimeObject * __this, int8_t ___SByte1, int8_t ___SByte2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_310 (RuntimeObject * __this, uint32_t ___UInt321, uint32_t ___UInt322, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_311 (RuntimeObject * __this, Guid_t  ___Guid1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_312 (RuntimeObject * __this, Guid_t  ___Guid1, Guid_t  ___Guid2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_313 (RuntimeObject * __this, uint32_t ___UInt321, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_314 (RuntimeObject * __this, uint64_t ___UInt641, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_315 (RuntimeObject * __this, uint64_t ___UInt641, uint64_t ___UInt642, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_316 (RuntimeObject * __this, TileCoord_t51EDF1EA1A3A7F9C1D85C186E7A7954535C225BA  ___TileCoord1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_317 (RuntimeObject * __this, TileCoord_t51EDF1EA1A3A7F9C1D85C186E7A7954535C225BA  ___TileCoord1, TileCoord_t51EDF1EA1A3A7F9C1D85C186E7A7954535C225BA  ___TileCoord2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_318 (RuntimeObject * __this, PendingPlayer_t40459CEDE375BC08A48A97694355F960BF84967F  ___PendingPlayer1, PendingPlayer_t40459CEDE375BC08A48A97694355F960BF84967F  ___PendingPlayer2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_319 (RuntimeObject * __this, PendingPlayer_t40459CEDE375BC08A48A97694355F960BF84967F  ___PendingPlayer1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_320 (RuntimeObject * __this, Change_tA73B83D716F0F54FF7C49B3C2789779480325A58  ___Change1, Change_tA73B83D716F0F54FF7C49B3C2789779480325A58  ___Change2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_321 (RuntimeObject * __this, Change_tA73B83D716F0F54FF7C49B3C2789779480325A58  ___Change1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_322 (RuntimeObject * __this, Change_t70C8E48E2782F7DA0BC9C1518C4FAD99E6DB80DA  ___Change1, Change_t70C8E48E2782F7DA0BC9C1518C4FAD99E6DB80DA  ___Change2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_323 (RuntimeObject * __this, Change_t70C8E48E2782F7DA0BC9C1518C4FAD99E6DB80DA  ___Change1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_324 (RuntimeObject * __this, int8_t ___SByte1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_325 (RuntimeObject * __this, uint8_t ___Byte1, uint8_t ___Byte2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_326 (RuntimeObject * __this, uint8_t ___Byte1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_327 (RuntimeObject * __this, int16_t ___Int161, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_328 (RuntimeObject * __this, KeyValuePair_2_t5DDBBB9A3C8CBE3A4A39721E8F0A10AEBF13737B  ___KeyValuePair_21, KeyValuePair_2_t5DDBBB9A3C8CBE3A4A39721E8F0A10AEBF13737B  ___KeyValuePair_22, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_329 (RuntimeObject * __this, KeyValuePair_2_t5DDBBB9A3C8CBE3A4A39721E8F0A10AEBF13737B  ___KeyValuePair_21, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_330 (RuntimeObject * __this, KeyValuePair_2_t23481547E419E16E3B96A303578C1EB685C99EEE  ___KeyValuePair_21, KeyValuePair_2_t23481547E419E16E3B96A303578C1EB685C99EEE  ___KeyValuePair_22, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_331 (RuntimeObject * __this, KeyValuePair_2_t23481547E419E16E3B96A303578C1EB685C99EEE  ___KeyValuePair_21, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_332 (RuntimeObject * __this, float ___Single1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_333 (RuntimeObject * __this, OrderBlock_t3B2BBCE8320FAEC3DB605F7DC9AB641102F53727  ___OrderBlock1, OrderBlock_t3B2BBCE8320FAEC3DB605F7DC9AB641102F53727  ___OrderBlock2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_334 (RuntimeObject * __this, OrderBlock_t3B2BBCE8320FAEC3DB605F7DC9AB641102F53727  ___OrderBlock1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_335 (RuntimeObject * __this, Color32_t23ABC4AE0E0BDFD2E22EE1FA0DA3904FFE5F6E23  ___Color321, Color32_t23ABC4AE0E0BDFD2E22EE1FA0DA3904FFE5F6E23  ___Color322, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_336 (RuntimeObject * __this, Color32_t23ABC4AE0E0BDFD2E22EE1FA0DA3904FFE5F6E23  ___Color321, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_337 (RuntimeObject * __this, Color_t119BCA590009762C7223FDD3AF9706653AC84ED2  ___Color1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_338 (RuntimeObject * __this, RaycastResult_t991BCED43A91EDD8580F39631DA07B1F88C58B91  ___RaycastResult1, RaycastResult_t991BCED43A91EDD8580F39631DA07B1F88C58B91  ___RaycastResult2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_339 (RuntimeObject * __this, RaycastResult_t991BCED43A91EDD8580F39631DA07B1F88C58B91  ___RaycastResult1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_340 (RuntimeObject * __this, Quaternion_t319F3319A7D43FFA5D819AD6C0A98851F0095357  ___Quaternion1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_341 (RuntimeObject * __this, RaycastHit2D_t5E8A7F96317BAF2033362FC780F4D72DC72764BE  ___RaycastHit2D1, RaycastHit2D_t5E8A7F96317BAF2033362FC780F4D72DC72764BE  ___RaycastHit2D2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_342 (RuntimeObject * __this, RaycastHit2D_t5E8A7F96317BAF2033362FC780F4D72DC72764BE  ___RaycastHit2D1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_343 (RuntimeObject * __this, RenderTargetIdentifier_tB7480EE944FC70E0AB7D499DB17D119EB65B0F5B  ___RenderTargetIdentifier1, RenderTargetIdentifier_tB7480EE944FC70E0AB7D499DB17D119EB65B0F5B  ___RenderTargetIdentifier2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_344 (RuntimeObject * __this, RenderTargetIdentifier_tB7480EE944FC70E0AB7D499DB17D119EB65B0F5B  ___RenderTargetIdentifier1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_345 (RuntimeObject * __this, ColorBlock_t93B54DF6E8D65D24CEA9726CA745E48C53E3B1EA  ___ColorBlock1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_346 (RuntimeObject * __this, Navigation_t761250C05C09773B75F5E0D52DDCBBFE60288A07  ___Navigation1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_347 (RuntimeObject * __this, SpriteState_t58B9DD66A79CD69AB4CFC3AD0C41E45DC2192C0A  ___SpriteState1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_348 (RuntimeObject * __this, UICharInfo_tB4C92043A686A600D36A92E3108F173C499E318A  ___UICharInfo1, UICharInfo_tB4C92043A686A600D36A92E3108F173C499E318A  ___UICharInfo2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_349 (RuntimeObject * __this, UICharInfo_tB4C92043A686A600D36A92E3108F173C499E318A  ___UICharInfo1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_350 (RuntimeObject * __this, UILineInfo_t0AF27251CA07CEE2BC0C1FEF752245596B8033E6  ___UILineInfo1, UILineInfo_t0AF27251CA07CEE2BC0C1FEF752245596B8033E6  ___UILineInfo2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_351 (RuntimeObject * __this, UILineInfo_t0AF27251CA07CEE2BC0C1FEF752245596B8033E6  ___UILineInfo1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_352 (RuntimeObject * __this, UIVertex_t0583C35B730B218B542E80203F5F4BC6F1E9E577  ___UIVertex1, UIVertex_t0583C35B730B218B542E80203F5F4BC6F1E9E577  ___UIVertex2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_353 (RuntimeObject * __this, UIVertex_t0583C35B730B218B542E80203F5F4BC6F1E9E577  ___UIVertex1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_354 (RuntimeObject * __this, WorkRequest_t0247B62D135204EAA95FC0B2EC829CB27B433F94  ___WorkRequest1, WorkRequest_t0247B62D135204EAA95FC0B2EC829CB27B433F94  ___WorkRequest2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_355 (RuntimeObject * __this, WorkRequest_t0247B62D135204EAA95FC0B2EC829CB27B433F94  ___WorkRequest1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_356 (RuntimeObject * __this, Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  ___Vector21, Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  ___Vector22, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_357 (RuntimeObject * __this, Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  ___Vector21, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_358 (RuntimeObject * __this, Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  ___Vector31, Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  ___Vector32, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_359 (RuntimeObject * __this, Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  ___Vector31, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_360 (RuntimeObject * __this, Vector4_tD148D6428C3F8FF6CD998F82090113C2B490B76E  ___Vector41, Vector4_tD148D6428C3F8FF6CD998F82090113C2B490B76E  ___Vector42, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_361 (RuntimeObject * __this, Vector4_tD148D6428C3F8FF6CD998F82090113C2B490B76E  ___Vector41, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  PendingPlayer_t40459CEDE375BC08A48A97694355F960BF84967F  UnresolvedVirtualCall_362 (RuntimeObject * __this, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  Change_tA73B83D716F0F54FF7C49B3C2789779480325A58  UnresolvedVirtualCall_363 (RuntimeObject * __this, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  Change_t70C8E48E2782F7DA0BC9C1518C4FAD99E6DB80DA  UnresolvedVirtualCall_364 (RuntimeObject * __this, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  KeyValuePair_2_t5DDBBB9A3C8CBE3A4A39721E8F0A10AEBF13737B  UnresolvedVirtualCall_365 (RuntimeObject * __this, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  OrderBlock_t3B2BBCE8320FAEC3DB605F7DC9AB641102F53727  UnresolvedVirtualCall_366 (RuntimeObject * __this, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  Color32_t23ABC4AE0E0BDFD2E22EE1FA0DA3904FFE5F6E23  UnresolvedVirtualCall_367 (RuntimeObject * __this, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RaycastResult_t991BCED43A91EDD8580F39631DA07B1F88C58B91  UnresolvedVirtualCall_368 (RuntimeObject * __this, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RaycastHit2D_t5E8A7F96317BAF2033362FC780F4D72DC72764BE  UnresolvedVirtualCall_369 (RuntimeObject * __this, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RenderTargetIdentifier_tB7480EE944FC70E0AB7D499DB17D119EB65B0F5B  UnresolvedVirtualCall_370 (RuntimeObject * __this, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  UICharInfo_tB4C92043A686A600D36A92E3108F173C499E318A  UnresolvedVirtualCall_371 (RuntimeObject * __this, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  UILineInfo_t0AF27251CA07CEE2BC0C1FEF752245596B8033E6  UnresolvedVirtualCall_372 (RuntimeObject * __this, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  UIVertex_t0583C35B730B218B542E80203F5F4BC6F1E9E577  UnresolvedVirtualCall_373 (RuntimeObject * __this, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  WorkRequest_t0247B62D135204EAA95FC0B2EC829CB27B433F94  UnresolvedVirtualCall_374 (RuntimeObject * __this, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  UnresolvedVirtualCall_375 (RuntimeObject * __this, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  Vector4_tD148D6428C3F8FF6CD998F82090113C2B490B76E  UnresolvedVirtualCall_376 (RuntimeObject * __this, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  PendingPlayer_t40459CEDE375BC08A48A97694355F960BF84967F  UnresolvedVirtualCall_377 (RuntimeObject * __this, int32_t ___Int321, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_378 (RuntimeObject * __this, PendingPlayer_t40459CEDE375BC08A48A97694355F960BF84967F  ___PendingPlayer1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  Change_tA73B83D716F0F54FF7C49B3C2789779480325A58  UnresolvedVirtualCall_379 (RuntimeObject * __this, int32_t ___Int321, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_380 (RuntimeObject * __this, Change_tA73B83D716F0F54FF7C49B3C2789779480325A58  ___Change1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  Change_t70C8E48E2782F7DA0BC9C1518C4FAD99E6DB80DA  UnresolvedVirtualCall_381 (RuntimeObject * __this, int32_t ___Int321, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_382 (RuntimeObject * __this, Change_t70C8E48E2782F7DA0BC9C1518C4FAD99E6DB80DA  ___Change1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  uint8_t UnresolvedVirtualCall_383 (RuntimeObject * __this, int32_t ___Int321, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_384 (RuntimeObject * __this, uint8_t ___Byte1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  KeyValuePair_2_t5DDBBB9A3C8CBE3A4A39721E8F0A10AEBF13737B  UnresolvedVirtualCall_385 (RuntimeObject * __this, int32_t ___Int321, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_386 (RuntimeObject * __this, KeyValuePair_2_t5DDBBB9A3C8CBE3A4A39721E8F0A10AEBF13737B  ___KeyValuePair_21, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  KeyValuePair_2_t23481547E419E16E3B96A303578C1EB685C99EEE  UnresolvedVirtualCall_387 (RuntimeObject * __this, int32_t ___Int321, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_388 (RuntimeObject * __this, KeyValuePair_2_t23481547E419E16E3B96A303578C1EB685C99EEE  ___KeyValuePair_21, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_389 (RuntimeObject * __this, CustomAttributeNamedArgument_t08BA731A94FD7F173551DF3098384CB9B3056E9E  ___CustomAttributeNamedArgument1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_390 (RuntimeObject * __this, CustomAttributeNamedArgument_t08BA731A94FD7F173551DF3098384CB9B3056E9E  ___CustomAttributeNamedArgument1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_391 (RuntimeObject * __this, CustomAttributeTypedArgument_t238ACCB3A438CB5EDE4A924C637B288CCEC958E8  ___CustomAttributeTypedArgument1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int32_t UnresolvedVirtualCall_392 (RuntimeObject * __this, CustomAttributeTypedArgument_t238ACCB3A438CB5EDE4A924C637B288CCEC958E8  ___CustomAttributeTypedArgument1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  OrderBlock_t3B2BBCE8320FAEC3DB605F7DC9AB641102F53727  UnresolvedVirtualCall_393 (RuntimeObject * __this, int32_t ___Int321, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_394 (RuntimeObject * __this, OrderBlock_t3B2BBCE8320FAEC3DB605F7DC9AB641102F53727  ___OrderBlock1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  Color32_t23ABC4AE0E0BDFD2E22EE1FA0DA3904FFE5F6E23  UnresolvedVirtualCall_395 (RuntimeObject * __this, int32_t ___Int321, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_396 (RuntimeObject * __this, Color32_t23ABC4AE0E0BDFD2E22EE1FA0DA3904FFE5F6E23  ___Color321, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  Color_t119BCA590009762C7223FDD3AF9706653AC84ED2  UnresolvedVirtualCall_397 (RuntimeObject * __this, int32_t ___Int321, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_398 (RuntimeObject * __this, Color_t119BCA590009762C7223FDD3AF9706653AC84ED2  ___Color1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RaycastResult_t991BCED43A91EDD8580F39631DA07B1F88C58B91  UnresolvedVirtualCall_399 (RuntimeObject * __this, int32_t ___Int321, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_400 (RuntimeObject * __this, RaycastResult_t991BCED43A91EDD8580F39631DA07B1F88C58B91  ___RaycastResult1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RaycastHit2D_t5E8A7F96317BAF2033362FC780F4D72DC72764BE  UnresolvedVirtualCall_401 (RuntimeObject * __this, int32_t ___Int321, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_402 (RuntimeObject * __this, RaycastHit2D_t5E8A7F96317BAF2033362FC780F4D72DC72764BE  ___RaycastHit2D1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RenderTargetIdentifier_tB7480EE944FC70E0AB7D499DB17D119EB65B0F5B  UnresolvedVirtualCall_403 (RuntimeObject * __this, int32_t ___Int321, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_404 (RuntimeObject * __this, RenderTargetIdentifier_tB7480EE944FC70E0AB7D499DB17D119EB65B0F5B  ___RenderTargetIdentifier1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_405 (RuntimeObject * __this, UICharInfo_tB4C92043A686A600D36A92E3108F173C499E318A  ___UICharInfo1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_406 (RuntimeObject * __this, UILineInfo_t0AF27251CA07CEE2BC0C1FEF752245596B8033E6  ___UILineInfo1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_407 (RuntimeObject * __this, UIVertex_t0583C35B730B218B542E80203F5F4BC6F1E9E577  ___UIVertex1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  WorkRequest_t0247B62D135204EAA95FC0B2EC829CB27B433F94  UnresolvedVirtualCall_408 (RuntimeObject * __this, int32_t ___Int321, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_409 (RuntimeObject * __this, WorkRequest_t0247B62D135204EAA95FC0B2EC829CB27B433F94  ___WorkRequest1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  UnresolvedVirtualCall_410 (RuntimeObject * __this, int32_t ___Int321, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_411 (RuntimeObject * __this, Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  ___Vector21, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  UnresolvedVirtualCall_412 (RuntimeObject * __this, int32_t ___Int321, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_413 (RuntimeObject * __this, Vector3_tDCF05E21F632FE2BA260C06E0D10CA81513E6720  ___Vector31, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  Vector4_tD148D6428C3F8FF6CD998F82090113C2B490B76E  UnresolvedVirtualCall_414 (RuntimeObject * __this, int32_t ___Int321, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_415 (RuntimeObject * __this, Vector4_tD148D6428C3F8FF6CD998F82090113C2B490B76E  ___Vector41, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  ArraySegment_1_t5B17204266E698CC035E2A7F6435A4F78286D0FA  UnresolvedVirtualCall_416 (RuntimeObject * __this, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  VoidTaskResult_t66EBC10DDE738848DB00F6EC1A2536D7D4715F40  UnresolvedVirtualCall_417 (RuntimeObject * __this, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_418 (RuntimeObject * __this, KeyValuePair_2_t86464C52F9602337EAC68825E6BE06951D7530CE  ___KeyValuePair_21, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeObject * UnresolvedVirtualCall_419 (RuntimeObject * __this, KeyValuePair_2_t23481547E419E16E3B96A303578C1EB685C99EEE  ___KeyValuePair_21, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  ArraySegment_1_t5B17204266E698CC035E2A7F6435A4F78286D0FA  UnresolvedVirtualCall_420 (RuntimeObject * __this, RuntimeObject * ___Object1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  Nullable_1_t0D03270832B3FFDDC0E7C2D89D4A0EA25376A1EB  UnresolvedVirtualCall_421 (RuntimeObject * __this, RuntimeObject * ___Object1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  VoidTaskResult_t66EBC10DDE738848DB00F6EC1A2536D7D4715F40  UnresolvedVirtualCall_422 (RuntimeObject * __this, RuntimeObject * ___Object1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  int8_t UnresolvedVirtualCall_423 (RuntimeObject * __this, int32_t ___Int321, intptr_t ___IntPtr2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  VoidTaskResult_t66EBC10DDE738848DB00F6EC1A2536D7D4715F40  UnresolvedVirtualCall_424 (RuntimeObject * __this, RuntimeObject * ___Object1, RuntimeObject * ___Object2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  Matrix4x4_t6BF60F70C9169DF14C9D2577672A44224B236ECA  UnresolvedVirtualCall_425 (RuntimeObject * __this, RuntimeObject * ___Object1, Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  ___Vector22, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeObject * UnresolvedVirtualCall_426 (RuntimeObject * __this, RuntimeObject * ___Object1, ReadWriteParameters_t5A9E416E0129249869039FC606326558DA3B597F  ___ReadWriteParameters2, RuntimeObject * ___Object3, RuntimeObject * ___Object4, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  RuntimeObject * UnresolvedVirtualCall_427 (RuntimeObject * __this, RuntimeObject * ___Object1, int32_t ___Int322, RuntimeObject * ___Object3, RuntimeObject * ___Object4, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_428 (RuntimeObject * __this, Scene_t942E023788C2BC9FBB7EC8356B4FB0088B2CFED2  ___Scene1, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_429 (RuntimeObject * __this, Scene_t942E023788C2BC9FBB7EC8356B4FB0088B2CFED2  ___Scene1, int32_t ___Int322, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_430 (RuntimeObject * __this, Scene_t942E023788C2BC9FBB7EC8356B4FB0088B2CFED2  ___Scene1, Scene_t942E023788C2BC9FBB7EC8356B4FB0088B2CFED2  ___Scene2, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_431 (RuntimeObject * __this, int8_t ___SByte1, int8_t ___SByte2, float ___Single3, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_432 (RuntimeObject * __this, int32_t ___Int321, int32_t ___Int322, float ___Single3, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_433 (RuntimeObject * __this, float ___Single1, float ___Single2, float ___Single3, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_434 (RuntimeObject * __this, Color_t119BCA590009762C7223FDD3AF9706653AC84ED2  ___Color1, Color_t119BCA590009762C7223FDD3AF9706653AC84ED2  ___Color2, float ___Single3, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_435 (RuntimeObject * __this, Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  ___Vector21, Vector2_tA85D2DD88578276CA8A8796756458277E72D073D  ___Vector22, float ___Single3, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

static  void UnresolvedVirtualCall_436 (RuntimeObject * __this, Vector4_tD148D6428C3F8FF6CD998F82090113C2B490B76E  ___Vector41, Vector4_tD148D6428C3F8FF6CD998F82090113C2B490B76E  ___Vector42, float ___Single3, const RuntimeMethod* method)
{
	il2cpp_codegen_raise_execution_engine_exception(method);
	il2cpp_codegen_no_return();
}

extern const Il2CppMethodPointer g_UnresolvedVirtualMethodPointers[];
const Il2CppMethodPointer g_UnresolvedVirtualMethodPointers[437] = 
{
	(const Il2CppMethodPointer) UnresolvedVirtualCall_0,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_1,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_2,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_3,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_4,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_5,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_6,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_7,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_8,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_9,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_10,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_11,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_12,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_13,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_14,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_15,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_16,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_17,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_18,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_19,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_20,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_21,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_22,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_23,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_24,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_25,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_26,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_27,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_28,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_29,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_30,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_31,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_32,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_33,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_34,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_35,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_36,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_37,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_38,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_39,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_40,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_41,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_42,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_43,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_44,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_45,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_46,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_47,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_48,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_49,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_50,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_51,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_52,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_53,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_54,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_55,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_56,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_57,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_58,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_59,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_60,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_61,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_62,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_63,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_64,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_65,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_66,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_67,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_68,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_69,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_70,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_71,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_72,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_73,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_74,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_75,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_76,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_77,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_78,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_79,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_80,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_81,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_82,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_83,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_84,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_85,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_86,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_87,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_88,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_89,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_90,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_91,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_92,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_93,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_94,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_95,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_96,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_97,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_98,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_99,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_100,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_101,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_102,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_103,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_104,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_105,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_106,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_107,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_108,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_109,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_110,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_111,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_112,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_113,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_114,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_115,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_116,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_117,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_118,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_119,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_120,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_121,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_122,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_123,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_124,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_125,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_126,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_127,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_128,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_129,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_130,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_131,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_132,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_133,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_134,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_135,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_136,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_137,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_138,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_139,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_140,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_141,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_142,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_143,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_144,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_145,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_146,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_147,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_148,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_149,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_150,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_151,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_152,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_153,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_154,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_155,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_156,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_157,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_158,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_159,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_160,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_161,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_162,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_163,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_164,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_165,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_166,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_167,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_168,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_169,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_170,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_171,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_172,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_173,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_174,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_175,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_176,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_177,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_178,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_179,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_180,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_181,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_182,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_183,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_184,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_185,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_186,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_187,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_188,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_189,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_190,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_191,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_192,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_193,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_194,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_195,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_196,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_197,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_198,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_199,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_200,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_201,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_202,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_203,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_204,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_205,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_206,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_207,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_208,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_209,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_210,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_211,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_212,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_213,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_214,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_215,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_216,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_217,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_218,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_219,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_220,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_221,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_222,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_223,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_224,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_225,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_226,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_227,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_228,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_229,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_230,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_231,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_232,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_233,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_234,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_235,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_236,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_237,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_238,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_239,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_240,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_241,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_242,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_243,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_244,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_245,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_246,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_247,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_248,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_249,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_250,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_251,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_252,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_253,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_254,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_255,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_256,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_257,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_258,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_259,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_260,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_261,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_262,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_263,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_264,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_265,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_266,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_267,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_268,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_269,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_270,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_271,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_272,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_273,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_274,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_275,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_276,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_277,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_278,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_279,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_280,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_281,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_282,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_283,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_284,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_285,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_286,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_287,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_288,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_289,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_290,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_291,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_292,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_293,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_294,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_295,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_296,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_297,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_298,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_299,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_300,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_301,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_302,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_303,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_304,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_305,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_306,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_307,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_308,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_309,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_310,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_311,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_312,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_313,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_314,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_315,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_316,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_317,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_318,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_319,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_320,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_321,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_322,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_323,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_324,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_325,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_326,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_327,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_328,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_329,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_330,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_331,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_332,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_333,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_334,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_335,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_336,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_337,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_338,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_339,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_340,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_341,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_342,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_343,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_344,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_345,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_346,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_347,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_348,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_349,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_350,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_351,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_352,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_353,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_354,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_355,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_356,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_357,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_358,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_359,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_360,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_361,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_362,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_363,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_364,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_365,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_366,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_367,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_368,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_369,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_370,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_371,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_372,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_373,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_374,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_375,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_376,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_377,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_378,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_379,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_380,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_381,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_382,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_383,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_384,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_385,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_386,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_387,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_388,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_389,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_390,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_391,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_392,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_393,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_394,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_395,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_396,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_397,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_398,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_399,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_400,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_401,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_402,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_403,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_404,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_405,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_406,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_407,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_408,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_409,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_410,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_411,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_412,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_413,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_414,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_415,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_416,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_417,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_418,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_419,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_420,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_421,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_422,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_423,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_424,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_425,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_426,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_427,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_428,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_429,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_430,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_431,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_432,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_433,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_434,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_435,
	(const Il2CppMethodPointer) UnresolvedVirtualCall_436,
};
